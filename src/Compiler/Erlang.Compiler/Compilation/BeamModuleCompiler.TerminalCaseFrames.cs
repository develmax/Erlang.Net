using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private readonly record struct VirtualOperand(int? Value, Term? Literal);

        private sealed record TerminalConstruction(int Result, bool Cons, VirtualOperand[] Inputs);
        private sealed record VirtualTerminalCall(
            Expr Source,
            string? Module,
            VirtualOperand[] Arguments,
            int Result,
            TerminalConstruction[] Constructions
        );
        private sealed record TerminalCallTransfer(
            Expr Source,
            string? Module,
            int OperandCount,
            List<(int Destination, int Source)> Moves,
            List<(int Destination, Term Value)> Constants,
            List<(int Destination, int Source)> SavedInputs
        );

        private sealed record AllocatedTerminalCall(
            TerminalCallTransfer Entry,
            int? RetainedResultSlot,
            bool Tail,
            int Trim,
            int RemainingSlots,
            (int Source, int Destination)[] TrimMoves,
            int? DeferredArgumentSlot,
            TerminalConstructionPlan? Constructions,
            TerminalProjection[] Projections,
            int? HeapLiveBeforeProjections
        );

        private sealed record VirtualTerminalCaseArm(
            Atom? Key,
            VirtualTerminalCall[] Calls,
            bool Tail,
            BeamFrameValueLiveness.Operation[] Operations,
            TerminalConstruction[] Constructions,
            VirtualOperand Result
        )
        {
            internal bool RequiresFrame => Calls.Length > (Tail ? 1 : 0);
        }

        private sealed record TerminalCaseBranch(
            AllocatedTerminalCall[] Calls,
            int[] EntrySpills,
            int FrameLive,
            TerminalProjection[] Projections,
            int RemainingSlots,
            TerminalConstructionPlan ReturnConstructions,
            TerminalProjection[] ReturnProjections,
            int? ReturnHeapLiveBeforeProjections,
            VirtualOperand Result
        );

        private sealed record InheritedTerminalFrame(
            int Width,
            IReadOnlyDictionary<int, (int Tag, int Register)> Homes
        );

        private sealed record TerminalConstructionPlan(
            (TerminalConstruction Operation, int Register)[] Schedule,
            IReadOnlyDictionary<int, (int Tag, int Register)> InitialValues,
            int Live,
            int HeapWords
        );

        private bool TryEmitPlannedTerminalCase(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || recursiveClosure is not null || function.Clauses is not
                [
                { Guard: null, Body: Expr.Case { Value: Expr.Variable selector } decision } clause
                ]
                || !HasUnconditionalParameters(clause) || decision.Clauses.Count < 2)
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(pattern => pattern.Name).ToArray();
            int selection = Array.IndexOf(parameters, selector.Name);
            if (selection < 0)
            {
                return false;
            }

            if (decision.Clauses[0].Patterns is [MapPattern])
            {
                return TryEmitPlannedTerminalMapCase(
                    function,
                    decision,
                    parameters,
                    selection
                );
            }

            var branches = new List<(VirtualTerminalCaseArm Arm, string[] Parameters)>();
            var keys = new HashSet<Atom>();
            foreach (Clause arm in decision.Clauses)
            {
                if (arm is not { Guard: null, Patterns: [Pattern.Literal { Value: Atom key }] }
                    || !keys.Add(key) || LowerTerminalCaseArm(
                        key,
                        arm.Body,
                        parameters,
                        allowFunctionValueCalls: true
                    ) is not { } plan)
                {
                    return false;
                }

                branches.Add((plan, parameters));
            }

            if (!EmitPlannedAtomBranches(
                selection,
                branches,
                null,
                out int failure,
                allocateFailureLabel: true
            ))
            {
                return false;
            }

            Mark(failure);
            BeamSourceLocation previous = location;
            owner.EmitArithmeticLine(decision.SourceLine, ref previous);
            owner.Op(CaseEnd, XRegisterTag, selection);

            return true;
        }

        private bool EmitPlannedAtomBranches(
            int selection,
            List<(VirtualTerminalCaseArm Arm, string[] Parameters)> branches,
            (VirtualTerminalCaseArm Arm, string[] Parameters)? remaining,
            out int failure,
            bool allocateFailureLabel = false,
            bool closedPair = false,
            bool exhaustive = false
        )
        {
            failure = owner.nextLabel - 2;
            var ordered = branches.OrderBy(branch => branch.Arm.Key!, Comparer<Atom>.Create(CompareAtoms)).ToArray();
            var arms = remaining is { } fallback ? [.. ordered, fallback] : ordered;
            if (!arms.Any(branch => branch.Arm.RequiresFrame))
            {
                return false;
            }

            const int entryBlock = 0;
            int failureBlock = ordered.Length + 1;
            var blocks = new List<BeamFrameBlock>
            {
                new(
                    entryBlock,
                    exhaustive
                        ? Enumerable.Range(1, ordered.Length).ToArray()
                        : [failureBlock, .. Enumerable.Range(1, ordered.Length)],
                    [],
                    RequiresFrame: false
                )
            };
            for (int index = 0; index < arms.Length; index++)
            {
                blocks.Add(new(
                    index + 1,
                    [],
                    [],
                    RequiresFrame: arms[index].Arm.RequiresFrame
                ));
            }

            if (remaining is null && !exhaustive)
            {
                blocks.Add(new(
                    failureBlock,
                    [],
                    [],
                    RequiresFrame: false
                ));
            }

            var framePlan = BeamFramePlacement.Place(entryBlock, blocks);
            if (framePlan is null || framePlan.Owners[entryBlock] is not null)
            {
                return false;
            }

            var allocated = new TerminalCaseBranch[arms.Length];
            for (int index = 0; index < arms.Length; index++)
            {
                if (AllocateTerminalCaseRegisters(arms[index].Arm, arms[index].Parameters) is not { } branch)
                {
                    return false;
                }

                allocated[index] = branch;
            }

            if (closedPair)
            {
                int alternative = Label();
                owner.Op(ExactEqualTest, LabelTag, alternative);
                owner.Operand(XRegisterTag, selection);
                owner.LiteralOperand(ordered[1].Arm.Key!);
                EmitTerminalCaseArm(allocated[1], framePlan.Owners[2] is not null);
                Mark(alternative);
                EmitTerminalCaseArm(allocated[0], framePlan.Owners[1] is not null);

                return true;
            }

            int[] representatives = Enumerable.Range(0, ordered.Length).ToArray();
            for (int index = 1; index < representatives.Length; index++)
            {
                for (int earlier = 0; earlier < index; earlier++)
                {
                    if (representatives[earlier] == earlier && CanShareLocalTail(allocated[earlier], allocated[index]))
                    {
                        representatives[index] = earlier;
                        break;
                    }
                }
            }

            int[] labels = ordered.Length == 1 ? [] : new int[ordered.Length];
            for (int index = labels.Length - 1; index >= 0; index--)
            {
                if (representatives[index] == index)
                {
                    labels[index] = Label();
                }
            }

            for (int index = 0; index < labels.Length; index++)
            {
                labels[index] = labels[representatives[index]];
            }

            if (exhaustive)
            {
                // With no reachable miss, OTP uses the first sorted key's block
                // as the default and removes that key from the switch table.
                failure = labels[0];
            }
            else if (remaining is not null || allocateFailureLabel)
            {
                failure = Label();
            }

            if (ordered.Length == 1)
            {
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.Operand(XRegisterTag, selection);
                owner.LiteralOperand(ordered[0].Arm.Key!);
            }
            else
            {
                owner.EmitMembershipSelect(
                    selection,
                    failure,
                    ordered.Select((branch, index) => ((Term)branch.Arm.Key!, labels[index]))
                        .Skip(exhaustive ? 1 : 0)
                );
            }

            for (int index = ordered.Length - 1; index >= 0; index--)
            {
                if (representatives[index] != index)
                {
                    continue;
                }

                if (labels.Length != 0)
                {
                    Mark(labels[index]);
                }

                EmitTerminalCaseArm(
                    allocated[index],
                    framePlan.Owners[index + 1] is not null,
                    startLine: owner.lastEmittedLine
                );
            }

            if (remaining is not null)
            {
                Mark(failure);
                EmitTerminalCaseArm(
                    allocated[^1],
                    framePlan.Owners[failureBlock] is not null
                );
            }

            return true;
        }

        private static bool CanShareLocalTail(TerminalCaseBranch left, TerminalCaseBranch right)
        {
            // beam_jump shares identical blocks. A local tail transfer has
            // no line instruction, frame, heap operation or other effect here.
            return left is { Calls: [{ Tail: true, Entry.Module: null, Constructions: null } a], EntrySpills.Length: 0 }
                && right is { Calls: [{ Tail: true, Entry.Module: null, Constructions: null } b], EntrySpills.Length: 0 }
                && a.Entry.Source is Expr.Call leftCall && b.Entry.Source is Expr.Call rightCall
                && leftCall.Function == rightCall.Function
                && leftCall.Arguments.Count == rightCall.Arguments.Count
                && a.Entry.Moves.SequenceEqual(b.Entry.Moves)
                && a.Entry.Constants.SequenceEqual(b.Entry.Constants);
        }

        internal static bool MayPlanStraightLineValues(Clause clause) =>
            clause is { Guard: null, Body: Expr.Tuple or Expr.List or Expr.Sequence or Expr.Block }
            && clause.Patterns.Count < FirstReservedXRegister && HasUnconditionalParameters(clause);

        internal bool TryEmitStraightLineValues(Clause clause, FunctionDefinition? function = null)
        {
            if (ReadStraightLineValuePlan(clause) is not { } plan)
            {
                return false;
            }

            // The specific recipes keep their established argument and heap schedules.
            if (TryEmitEntryDynamicApplyTail(clause) || TryEmitConstructedCallContinuation(clause))
            {
                return true;
            }

            if (function is not null && TryEmitScrolledDetailPanel(function))
            {
                return true;
            }

            EmitTerminalCaseArm(plan.Branch, plan.Frame);

            return true;
        }

        internal IEnumerable<(string Name, int Arity)> ReadStraightLineLocalTargets(Clause clause)
        {
            if (ReadStraightLineValuePlan(clause) is not { } plan)
            {
                yield break;
            }

            foreach (var call in plan.Branch.Calls)
            {
                if (call.Entry is { Module: null, Source: Expr.Call local })
                {
                    yield return (local.Function, local.Arguments.Count);
                }
            }
        }

        private (TerminalCaseBranch Branch, bool Frame)? ReadStraightLineValuePlan(Clause clause)
        {
            if (owner.ModuleOptimizationDisabled || recursiveClosure is not null || !MayPlanStraightLineValues(clause))
            {
                return null;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (LowerTerminalCaseArm(null, clause.Body, parameters) is not { } arm)
            {
                return null;
            }

            if (arm.Calls.Length == 0)
            {
                if (arm is not { Constructions.Length: > 1, Result.Value: int result })
                {
                    return null;
                }

                var live = new HashSet<int> { result };
                foreach (TerminalConstruction construction in arm.Constructions.Reverse())
                {
                    if (!live.Remove(construction.Result))
                    {
                        return null;
                    }

                    live.UnionWith(ReadValues(construction.Inputs));
                }
            }
            else if (!owner.SsaTypeInferenceEnabled || arm.Calls.Length < 2
                || !arm.Calls.Any(call => call.Constructions.Length != 0))
            {
                return null;
            }

            if (AllocateTerminalCaseRegisters(arm, parameters) is not { } plan)
            {
                return null;
            }

            return (plan, arm.RequiresFrame);
        }

        private VirtualTerminalCaseArm? LowerTerminalCaseArm(
            Atom? key,
            Expr body,
            string[] parameters,
            string? constantAlias = null,
            Term? aliasValue = null,
            bool lowerAppend = false,
            bool pureConstructionOnly = false,
            bool allowFunctionValueCalls = false
        )
        {
            var constructions = new List<TerminalConstruction>();
            var deferred = new Dictionary<int, TerminalConstruction>();
            var calls = new List<VirtualTerminalCall>();
            var bindings = new Dictionary<string, VirtualOperand>(StringComparer.Ordinal);
            for (int index = 0; index < parameters.Length; index++)
            {
                if (parameters[index] != VariableScopeNames.Wildcard)
                {
                    bindings.Add(parameters[index], new(index, null));
                }
            }

            if (constantAlias is not null && bindings.ContainsKey(constantAlias))
            {
                bindings[constantAlias] = new(null, aliasValue ?? key);
            }

            int nextValue = parameters.Length;
            if (Lower(body) is not { } result)
            {
                return null;
            }

            ForceConstruction(result);
            // Even when its result is returned, call_fun needs a frame and
            // has no tail opcode. Its return remains in the terminal suffix.
            bool tail = calls.Count != 0 && calls[^1].Source is Expr.Call
                && result.Value == calls[^1].Result;
            if (tail && calls.Count > 1 && owner.SsaTypeInferenceEnabled && !owner.ModuleOptimizationDisabled
                && owner.OnLoadFunction is null)
            {
                VirtualTerminalCall terminal = calls[^1];
                var knownArguments = new Dictionary<int, Atom>();
                foreach (VirtualTerminalCall producer in calls.Take(calls.Count - 1))
                {
                    if (producer is not { Module: null, Source: Expr.Call local }
                        || !terminal.Arguments.Any(argument => argument.Value == producer.Result))
                    {
                        continue;
                    }

                    var signature = (local.Function, local.Arguments.Count);
                    if (!owner.IsDeclaredNative(signature) && localFunctions.TryGetValue(signature, out var definition)
                        && !definition.IsInlineFailure && ReadSingletonResult(definition) is Atom constant)
                    {
                        knownArguments.Add(producer.Result, constant);
                    }
                }

                if (knownArguments.Count != 0)
                {
                    // Keep preceding calls, including effects and failures. Only the
                    // operands of the reachable tail transfer use the proven result.
                    calls[^1] = terminal with
                    {
                        Arguments = terminal.Arguments.Select(argument => argument.Value is int value
                            && knownArguments.TryGetValue(value, out Atom? constant) ? new(null, constant) : argument).ToArray()
                    };
                }
            }

            if (AssignConstructionRegions(calls, constructions, result) is not { } returns)
            {
                return null;
            }

            var uses = new List<BeamFrameValueLiveness.Operation>();
            foreach (VirtualTerminalCall call in calls)
            {
                AddConstructionUses(call.Constructions);
                uses.Add(new(
                    ReadValues(call.Arguments).ToArray(),
                    call.Result,
                    ClobbersXRegisters: true
                ));
            }

            AddConstructionUses(returns);
            uses.Add(new(ReadValues([result]).ToArray(), null, ClobbersXRegisters: false));

            return new(
                key,
                calls.ToArray(),
                tail,
                uses.ToArray(),
                returns,
                result
            );

            void AddConstructionUses(IEnumerable<TerminalConstruction> scheduled)
            {
                foreach (TerminalConstruction construction in scheduled)
                {
                    uses.Add(new(
                        ReadValues(construction.Inputs).ToArray(),
                        construction.Result,
                        ClobbersXRegisters: false
                    ));
                }
            }

            VirtualOperand? Lower(Expr expression)
            {
                if (AttributeLiteral.TryNormalizeConstant(expression, out Term constant))
                {
                    return new(null, constant);
                }

                switch (expression)
                {
                    case Expr.Variable variable:
                        return bindings.TryGetValue(variable.Name, out VirtualOperand value) ? value : null;
                    case Expr.Match { Pattern: Pattern.Variable variable } matched:
                        if (Lower(matched.Value) is not { } bound || variable.Name != VariableScopeNames.Wildcard
                            && !bindings.TryAdd(variable.Name, bound))
                        {
                            return null;
                        }

                        ForceConstruction(bound);

                        return bound;
                    case Expr.Sequence sequence:
                        VirtualOperand? current = null;
                        foreach (Expr item in sequence.Items)
                        {
                            if ((current = Lower(item)) is not { } evaluated)
                            {
                                return null;
                            }

                            ForceConstruction(evaluated);
                        }

                        return current;
                    case Expr.Block block:
                        return Lower(block.Body);
                    case Expr.Binary { Operator: ErlangOperators.Append, Left: Expr.List { Tail: null } list, Right: var appendTail } when lowerAppend:
                        return Lower(list with { Tail = appendTail });
                    case Expr.Binary { Operator: ErlangOperators.Append } append when lowerAppend:
                        return Lower(new Expr.Call(ErlangModuleNames.Module, ErlangOperators.Append, [append.Left, append.Right])
                        {
                            SourceLine = append.SourceLine
                        });
                    case Expr.Call requested when !pureConstructionOnly && TryResolveOrdinaryCallTarget(requested, out string? module):
                        if (LowerItems(requested.Arguments) is not { } operands)
                        {
                            return null;
                        }

                        int callResult = nextValue++;
                        calls.Add(new(
                            requested,
                            module,
                            operands,
                            callResult,
                            []
                        ));

                        return new(callResult, null);
                    case Expr.Apply { Function: Expr.Variable function } apply
                        when !pureConstructionOnly && allowFunctionValueCalls
                            && apply.Arguments.Count <= BeamWire.MaximumArity
                            && apply.Arguments.All(argument => argument is Expr.Variable or Expr.Literal):
                        if (Lower(function) is not { } target || LowerItems(apply.Arguments) is not { } arguments)
                        {
                            return null;
                        }

                        int applyResult = nextValue++;
                        calls.Add(new(
                            apply,
                            null,
                            [.. arguments, target],
                            applyResult,
                            []
                        ));

                        return new(applyResult, null);
                    case Expr.RecordConstruction record when pureConstructionOnly:
                        return Lower(RecordExpressions.Create(record));
                    case Expr.Tuple tuple:
                        if (LowerItems(tuple.Items) is not { } fields)
                        {
                            return null;
                        }

                        return Construct(cons: false, fields);
                    case Expr.List list:
                        if (LowerItems(list.Items, forceRoot: false) is not { } items)
                        {
                            return null;
                        }

                        VirtualOperand? tail = list.Tail is null ? new VirtualOperand(null, Nil.Value) : Lower(list.Tail);
                        if (tail is not { } rest)
                        {
                            return null;
                        }

                        for (int index = items.Length - 1; index >= 0; index--)
                        {
                            // Core-to-SSA evaluates head and tail prefixes in source
                            // order, then forces the tail root before the head root.
                            ForceConstruction(rest);
                            ForceConstruction(items[index]);
                            rest = Construct(cons: true, [items[index], rest]);
                        }

                        return rest;
                    default:
                        return null;
                }
            }

            VirtualOperand[]? LowerItems(IReadOnlyList<Expr> items, bool forceRoot = true)
            {
                var operands = new VirtualOperand[items.Count];
                for (int index = 0; index < items.Count; index++)
                {
                    if (Lower(items[index]) is not { } item)
                    {
                        return null;
                    }

                    if (forceRoot)
                    {
                        ForceConstruction(item);
                    }

                    operands[index] = item;
                }

                return operands;
            }

            VirtualOperand Construct(bool cons, VirtualOperand[] operands)
            {
                if (operands.All(operand => operand.Literal is not null))
                {
                    Term[] terms = operands.Select(operand => operand.Literal!).ToArray();

                    return new(null, cons ? new Cons(terms[0], terms[1]) : Term.Tuple(terms));
                }

                int value = nextValue++;
                deferred.Add(value, new(value, cons, operands));

                return new(value, null);
            }

            void ForceConstruction(VirtualOperand operand)
            {
                if (operand.Value is int value && deferred.Remove(value, out TerminalConstruction? pending))
                {
                    constructions.Add(pending);
                }
            }
        }

        private static TerminalConstruction[]? AssignConstructionRegions(
            List<VirtualTerminalCall> calls,
            List<TerminalConstruction> constructions,
            VirtualOperand result
        )
        {
            var byResult = constructions.ToDictionary(construction => construction.Result);
            if (!calls.Any(call => ReadValues(call.Arguments).Any(byResult.ContainsKey)))
            {
                return constructions.ToArray();
            }

            // A heap region ends at each ordinary call. Place pure
            // prerequisites at the region of their consumer without moving
            // the calls. Shared nodes across clobbers need a Y owner.
            var regions = new Dictionary<int, int>();
            for (int region = 0; region < calls.Count; region++)
            {
                if (!Assign(calls[region].Arguments, region))
                {
                    return null;
                }
            }

            if (!Assign([result], calls.Count))
            {
                return null;
            }

            var ordered = constructions.Where(construction => regions.ContainsKey(construction.Result))
                .ToLookup(construction => regions[construction.Result]);
            for (int region = 0; region < calls.Count; region++)
            {
                calls[region] = calls[region] with { Constructions = ordered[region].ToArray() };
            }

            return ordered[calls.Count].ToArray();

            bool Assign(VirtualOperand[] outputs, int region)
            {
                var pending = new Stack<int>(ReadValues(outputs));
                while (pending.TryPop(out int value))
                {
                    if (!byResult.TryGetValue(value, out TerminalConstruction? construction))
                    {
                        continue;
                    }

                    if (regions.TryGetValue(value, out int prior))
                    {
                        if (prior != region)
                        {
                            return false;
                        }

                        continue;
                    }

                    regions.Add(value, region);
                    foreach (int input in ReadValues(construction.Inputs))
                    {
                        pending.Push(input);
                    }
                }

                return true;
            }
        }

        private TerminalCaseBranch? AllocateTerminalCaseRegisters(
            VirtualTerminalCaseArm arm,
            string[] parameters,
            int carrier = -1,
            TerminalField[]? fields = null,
            DeferredEntryTupleFields? deferredFields = null,
            InheritedTerminalFrame? inherited = null
        )
        {
            IEnumerable<int> entryValues = inherited is null ? Enumerable.Range(0, parameters.Length) : inherited.Homes.Keys;
            if (deferredFields is not null)
            {
                arm = deferredFields.Arm;
                fields = deferredFields.Entry;
                // These IDs are defined by a later checked projection.
                // They do not yet name initialized entry registers.
                entryValues = entryValues.Except(deferredFields.ByCall.Values.SelectMany(group => group)
                    .Concat(deferredFields.Returns).Select(field => field.Value));
            }

            var saved = BeamFrameValueLiveness.FindSavedValues(entryValues, arm.Operations);
            TerminalConstruction[] constructions = arm.Constructions;
            VirtualOperand result = arm.Result;
            var lastUse = ReadTerminalLastUses(arm);
            Dictionary<int, int> slots;
            int[] entrySpills;
            if (inherited is null)
            {
                (slots, entrySpills) = ReserveTerminalYSlots(
                    arm,
                    saved,
                    lastUse,
                    parameters.Length
                );
            }
            else
            {
                slots = inherited.Homes.Where(pair => pair.Value.Tag == YRegisterTag && lastUse.ContainsKey(pair.Key))
                    .ToDictionary(pair => pair.Key, pair => pair.Value.Register);
                if (saved.Any(value => !slots.ContainsKey(value)))
                {
                    return null;
                }

                // These are existing frame homes. No entry spills are emitted
                // and a branch may not reserve a new Y interval in this frame.
                entrySpills = Enumerable.Repeat(-1, inherited.Width).ToArray();
            }
            int[] slotEnd = Enumerable.Repeat(-1, entrySpills.Length).ToArray();
            foreach (var (value, slot) in slots)
            {
                slotEnd[slot] = Math.Max(slotEnd[slot], lastUse[value]);
            }
            var values = inherited is null
                ? entryValues.ToDictionary(value => value, value => (Tag: XRegisterTag, Register: value))
                : new Dictionary<int, (int Tag, int Register)>(inherited.Homes);
            int frameLive = 0;
            TerminalProjection[] projections = [];
            var calls = new List<AllocatedTerminalCall>();
            int? pendingResult = null;
            int removed = 0;
            int[] callPositions = TerminalCallPositions(arm);
            foreach (VirtualTerminalCall requested in arm.Calls)
            {
                TerminalConstructionPlan? prefix = null;
                int[] callUses = calls.Count == 0
                    ? ReadValues(requested.Arguments).Concat(
                        requested.Constructions.SelectMany(construction => ReadValues(construction.Inputs))
                    ).Where(value => value < parameters.Length).Distinct().ToArray()
                    : [];
                bool allocatedEntry = calls.Count == 0 && fields is not null
                    && (requested.Constructions.Length != 0 || deferredFields is not null);
                if (allocatedEntry)
                {
                    projections = PlanProjectedConstructionInputs(
                        fields!,
                        entrySpills,
                        carrier,
                        callUses,
                        values,
                        requested
                    );
                }

                TerminalProjection[] delayed = [];
                int? heapLiveBeforeProjections = null;
                if (deferredFields is not null && deferredFields.ByCall.TryGetValue(calls.Count, out TerminalField[]? consumed))
                {
                    var used = requested.Arguments.Concat(requested.Constructions.SelectMany(value => value.Inputs));
                    if (AllocateDeferredTupleFields(
                        consumed,
                        requested.Arguments,
                        used,
                        values,
                        carrier,
                        removed,
                        pendingResult.HasValue
                    ) is not { } projected)
                    {
                        return null;
                    }

                    delayed = projected;
                    if (requested.Constructions.Length != 0)
                    {
                        int incoming = arm.Calls[calls.Count - 1].Result;
                        bool live = pendingResult.HasValue || ReadValues(requested.Arguments).Contains(incoming)
                            || requested.Constructions.Any(construction => ReadValues(construction.Inputs).Contains(incoming));
                        heapLiveBeforeProjections = live ? 1 : 0;
                    }
                }

                if (requested.Constructions.Length != 0)
                {
                    // The preceding result can remain in X0 through this
                    // heap region. It is copied to its Y home before the call.
                    int? preserved = pendingResult.HasValue ? arm.Calls[calls.Count - 1].Result : null;
                    if (PlanTerminalConstructions(
                        requested.Constructions,
                        requested.Arguments,
                        values,
                        preferArguments: true,
                        preserved
                    ) is not { } prepared)
                    {
                        return null;
                    }

                    prefix = prepared with { InitialValues = TrimTerminalLocations(prepared.InitialValues, removed) };
                }

                if (PlanTerminalCall(requested, values) is not { } call
                    || pendingResult is int keptSlot && (prefix is not null || delayed.Length != 0)
                    && call.Moves.Any(move => move.Destination == 0)
                    && call.SavedInputs!.Any(input => input.Source == keptSlot))
                {
                    return null;
                }

                if (calls.Count == 0)
                {
                    if (fields is not null && !allocatedEntry)
                    {
                        if (AllocateTerminalProjections(
                            call,
                            requested.Arguments,
                            entrySpills,
                            carrier,
                            fields
                        ) is not { } plan)
                        {
                            return null;
                        }

                        (call, projections) = plan;
                    }

                    var fieldValues = fields?.Select(field => field.Value).ToHashSet();
                    frameLive = callUses.Concat(entrySpills.Where(value => value >= 0))
                        .Where(value => fieldValues?.Contains(value) != true)
                        .Append(carrier).DefaultIfEmpty(-1).Max() + 1;
                }

                foreach (var (value, register) in values.ToArray())
                {
                    if (slots.TryGetValue(value, out int slot))
                    {
                        values[value] = (YRegisterTag, slot);
                    }
                    else if (register.Tag == XRegisterTag)
                    {
                        values.Remove(value);
                    }
                }

                values.Add(requested.Result, (XRegisterTag, 0));
                bool tail = arm.Tail && requested.Result == arm.Calls[^1].Result;
                int trim = 0;
                int? deferred = null;
                (int Source, int Destination)[] trimMoves = [];
                int boundary = removed;
                while (boundary < slotEnd.Length && slotEnd[boundary] <= callPositions[calls.Count])
                {
                    boundary++;
                }

                if (!tail && pendingResult is int retained && retained >= boundary
                    && call.OperandCount is > 1 and < FirstReservedXRegister
                    && !call.Moves.Any(move => move.Source == call.OperandCount)
                    && call.SavedInputs!.Where(input => input.Destination == 0).Select(input => (int?)input.Source)
                        .FirstOrDefault() is int outgoing && outgoing >= removed && outgoing < boundary && outgoing != retained)
                {
                    trim = boundary - removed;
                    deferred = outgoing - removed;
                }
                else if (!tail && pendingResult is null && boundary > removed
                    && (call.SavedInputs!.Any(input => input.Source >= removed && input.Source < boundary)
                        || delayed.Any(projection => projection.Input + removed < boundary)))
                {
                    // The outgoing copies are the last reads of this prefix.
                    trim = boundary - removed;
                }

                if (!tail && pendingResult is null && calls.Count != 0 && trim == 0)
                {
                    bool pure = call.Module is { } module && call.Source is Expr.Call direct
                        && BeamTrimBifs.IsPure(module, direct.Function, direct.Arguments.Count);
                    int[] active = Enumerable.Range(removed, slotEnd.Length - removed).ToArray();
                    bool[] initialized = Enumerable.Repeat(true, slotEnd.Length).ToArray();
                    if (PlanCallFrameTrim(
                        active,
                        slotEnd,
                        initialized,
                        callPositions[calls.Count - 1],
                        callPositions[calls.Count] + 1,
                        pure,
                        []
                    ) is { Moves.Length: > 0, Kills.Length: 0 } relocation)
                    {
                        trim = relocation.Removed;
                        trimMoves = relocation.Moves;
                        foreach (var (source, destination) in trimMoves)
                        {
                            int originalSource = source + removed;
                            int originalDestination = destination + removed;
                            slotEnd[originalDestination] = slotEnd[originalSource];
                            foreach (int value in slots.Keys.Where(value => slots[value] == originalSource).ToArray())
                            {
                                slots[value] = originalDestination;
                                if (values.TryGetValue(value, out var location) && location.Tag == YRegisterTag)
                                {
                                    values[value] = (YRegisterTag, originalDestination);
                                }
                            }
                        }
                    }
                }

                call = call with
                {
                    SavedInputs = call.SavedInputs!.Select(input => (input.Destination, input.Source - removed)).ToList()
                };
                calls.Add(new(
                    call,
                    pendingResult - removed,
                    tail,
                    trim,
                    slotEnd.Length - removed - trim,
                    trimMoves,
                    deferred,
                    prefix,
                    delayed,
                    heapLiveBeforeProjections
                ));
                removed += trim;
                pendingResult = slots.TryGetValue(requested.Result, out int savedSlot) ? savedSlot : null;
            }

            TerminalProjection[] returnProjections = [];
            int? returnHeapLiveBeforeProjections = null;
            if (deferredFields is { Returns.Length: > 0 })
            {
                VirtualOperand[] uses = [result, .. constructions.SelectMany(construction => construction.Inputs)];
                bool retainResult = ReadValues(uses).Contains(arm.Calls[^1].Result);
                if (AllocateDeferredTupleFields(
                    deferredFields.Returns,
                    [result],
                    uses,
                    values,
                    carrier,
                    removed,
                    retainResult
                ) is not { } projected)
                {
                    return null;
                }

                returnProjections = projected;
                returnHeapLiveBeforeProjections = retainResult ? 1 : 0;
            }

            var initial = TrimTerminalLocations(values, removed);
            if (PlanTerminalConstructions(constructions, [result], initial) is not { } suffix)
            {
                return null;
            }

            return new(
                calls.ToArray(),
                entrySpills,
                frameLive,
                projections,
                entrySpills.Length - removed,
                suffix,
                returnProjections,
                returnHeapLiveBeforeProjections,
                result
            );
        }

        private static int[] TerminalCallPositions(VirtualTerminalCaseArm arm) =>
            arm.Operations.Select((operation, index) => (operation, index))
                .Where(pair => pair.operation.ClobbersXRegisters)
                .Select(pair => pair.index)
                .ToArray();

        private static Dictionary<int, (int Tag, int Register)> TrimTerminalLocations(
            IReadOnlyDictionary<int, (int Tag, int Register)> values,
            int removed
        ) => values.Where(pair => pair.Value.Tag != YRegisterTag || pair.Value.Register >= removed)
            .ToDictionary(pair => pair.Key, pair => pair.Value.Tag == YRegisterTag
                ? (Tag: YRegisterTag, Register: pair.Value.Register - removed) : pair.Value);

        private static TerminalConstructionPlan? PlanTerminalConstructions(
            TerminalConstruction[] constructions,
            VirtualOperand[] required,
            Dictionary<int, (int Tag, int Register)> values,
            bool preferArguments = false,
            int? preserved = null
        )
        {
            var initial = new Dictionary<int, (int Tag, int Register)>(values);
            var constructionUse = new Dictionary<int, int>();
            for (int index = 0; index < constructions.Length; index++)
            {
                foreach (int value in ReadValues(constructions[index].Inputs))
                {
                    constructionUse[value] = index;
                }
            }

            foreach (int value in ReadValues(required))
            {
                constructionUse[value] = constructions.Length;
            }

            if (preserved is int kept)
            {
                constructionUse[kept] = constructions.Length;
            }

            int live = initial.Where(pair => constructionUse.ContainsKey(pair.Key) && pair.Value.Tag == XRegisterTag)
                .Select(pair => pair.Value.Register + 1).DefaultIfEmpty().Max();
            var preferred = new Dictionary<int, int>();
            if (preferArguments)
            {
                for (int argument = 0; argument < required.Length; argument++)
                {
                    if (required[argument].Value is int value)
                    {
                        preferred[value] = argument;
                    }
                }
            }

            var scheduled = new List<(TerminalConstruction Operation, int Register)>();
            int heap = 0;
            for (int index = 0; index < constructions.Length; index++)
            {
                TerminalConstruction operation = constructions[index];
                if (ReadValues(operation.Inputs).Any(value => !values.ContainsKey(value)))
                {
                    return null;
                }

                var occupied = values.Where(pair => pair.Value.Tag == XRegisterTag
                        && constructionUse.GetValueOrDefault(pair.Key, -1) > index)
                    .Select(pair => pair.Value.Register).ToHashSet();
                // The outgoing X register is preferred when the interval
                // fits. The heap check precedes this region's definitions.
                int register = preferred.TryGetValue(operation.Result, out int argumentRegister)
                    && !occupied.Contains(argumentRegister) ? argumentRegister : 0;
                while (occupied.Contains(register))
                {
                    register++;
                }

                if (register >= FirstReservedXRegister)
                {
                    return null;
                }

                values.Add(operation.Result, (XRegisterTag, register));
                scheduled.Add((operation, register));
                heap += operation.Cons ? ConsHeapWords : operation.Inputs.Length + 1;
            }

            if (ReadValues(required).Any(value => !values.ContainsKey(value)))
            {
                return null;
            }

            return new(
                scheduled.ToArray(),
                initial,
                live,
                heap
            );
        }

        private static Dictionary<int, int> ReadTerminalLastUses(VirtualTerminalCaseArm arm)
        {
            var lastUse = new Dictionary<int, int>();
            for (int operation = 0; operation < arm.Operations.Length; operation++)
            {
                foreach (int value in arm.Operations[operation].Uses)
                {
                    lastUse[value] = operation;
                }
            }

            return lastUse;
        }

        private static (Dictionary<int, int> Locations, int[] EntrySpills) ReserveTerminalYSlots(
            VirtualTerminalCaseArm arm,
            IReadOnlySet<int> saved,
            IReadOnlyDictionary<int, int> lastUse,
            int entryValueCount
        )
        {
            int[] callPositions = TerminalCallPositions(arm);
            var resultStores = arm.Calls.Select((call, index) => (call.Result, Store: callPositions[index] + 1))
                .ToDictionary(pair => pair.Result, pair => pair.Store);
            var intervals = saved.Select(value => (Value: value, Begin: resultStores.GetValueOrDefault(value), End: lastUse[value]))
                .OrderBy(interval => interval.Begin).ThenBy(interval => interval.Value);
            var unturned = new Dictionary<int, int>();
            var active = new Dictionary<int, int>();
            int width = 0;
            foreach (var interval in intervals)
            {
                foreach (var expired in active.Where(pair => pair.Value <= interval.Begin).ToArray())
                {
                    active.Remove(expired.Key);
                }

                int selected = 0;
                while (active.ContainsKey(selected))
                {
                    selected++;
                }

                active.Add(selected, interval.End);
                unturned.Add(interval.Value, selected);
                width = Math.Max(width, selected + 1);
            }

            // Y pools are numbered by allocation, then reversed for BEAM.
            var locations = unturned.ToDictionary(pair => pair.Key, pair => width - 1 - pair.Value);
            int[] entrySpills = Enumerable.Repeat(-1, width).ToArray();
            foreach (var (value, slot) in locations)
            {
                if (value < entryValueCount)
                {
                    entrySpills[slot] = value;
                }
            }

            return (locations, entrySpills);
        }

        private static TerminalCallTransfer? PlanTerminalCall(
            VirtualTerminalCall call,
            IReadOnlyDictionary<int, (int Tag, int Register)> values
        )
        {
            var moves = new List<(int Destination, int Source)>();
            var constants = new List<(int Destination, Term Value)>();
            var saved = new List<(int Destination, int Source)>();
            for (int destination = 0; destination < call.Arguments.Length; destination++)
            {
                var operand = call.Arguments[destination];
                if (operand.Value is not int value)
                {
                    constants.Add((destination, operand.Literal!));
                }
                else if (!values.TryGetValue(value, out var register))
                {
                    return null;
                }
                else if (register.Tag == YRegisterTag)
                {
                    saved.Add((destination, register.Register));
                }
                else if (register.Register != destination)
                {
                    moves.Add((destination, register.Register));
                }
            }

            return new(
                call.Source,
                call.Module,
                call.Arguments.Length,
                moves,
                constants,
                saved
            );
        }

        private static TerminalProjection[] PlanProjectedConstructionInputs(
            TerminalField[] fields,
            int[] savedValues,
            int carrier,
            int[] callUses,
            Dictionary<int, (int Tag, int Register)> locations,
            VirtualTerminalCall call
        )
        {
            var fieldValues = fields.Select(field => field.Value).ToHashSet();
            var used = callUses.ToHashSet();
            var occupied = callUses.Where(value => !fieldValues.Contains(value) && locations[value].Tag == XRegisterTag)
                .Select(value => locations[value].Register)
                .ToHashSet();
            var availableCopies = new Dictionary<int, int>();
            for (int slot = 0; slot < savedValues.Length; slot++)
            {
                int value = savedValues[slot];
                if (occupied.Contains(value) && !fieldValues.Contains(value))
                {
                    availableCopies.Add(value, slot);
                }
            }

            var projections = new List<TerminalProjection>();
            // Write a projection that reuses the carrier only after every
            // other extraction. A surviving field uses its existing Y home.
            foreach (TerminalField field in fields)
            {
                int slot = Array.IndexOf(savedValues, field.Value);
                int tag = YRegisterTag;
                int register = slot;
                if (slot < 0)
                {
                    if (!used.Contains(field.Value))
                    {
                        continue;
                    }

                    tag = XRegisterTag;
                    register = 0;
                    while (occupied.Contains(register))
                    {
                        // Use the established Y copy only when the selected
                        // temporary projection needs that physical X slot.
                        if (availableCopies.Remove(register, out int captured))
                        {
                            locations[register] = (YRegisterTag, captured);
                            break;
                        }

                        register++;
                    }

                    occupied.Add(register);
                }

                locations[field.Value] = (tag, register);
                projections.Add(new(
                    carrier,
                    tag,
                    register,
                    field.Opcode,
                    field.Index
                ));
            }

            for (int register = 0; register < call.Arguments.Length; register++)
            {
                if (!availableCopies.TryGetValue(register, out int home)
                    || call.Arguments[register].Value is not int built)
                {
                    continue;
                }

                int construction = Array.FindIndex(call.Constructions, operation => operation.Result == built);
                if (construction >= 0 && (call.Arguments.Any(argument => argument.Value == register)
                    || call.Constructions.Skip(construction + 1).Any(operation =>
                        operation.Inputs.Any(input => input.Value == register))))
                {
                    // A live input would block the outgoing affinity. Feed
                    // those reads from its Y copy and leave the X slot free.
                    locations[register] = (YRegisterTag, home);
                }
            }

            int carrierSlot = Array.IndexOf(savedValues, carrier);
            if (carrierSlot >= 0 && projections.All(projection => projection.Opcode == GetTupleElement))
            {
                bool readSavedCarrier = false;
                for (int index = 0; index < projections.Count; index++)
                {
                    TerminalProjection projection = projections[index];
                    if (readSavedCarrier)
                    {
                        projections[index] = projection with { Input = carrierSlot, InputTag = YRegisterTag };
                    }

                    readSavedCarrier |= projection.Tag == XRegisterTag && projection.Register == carrier;
                }

                return projections.ToArray();
            }

            return projections.OrderBy(projection => projection.Tag == XRegisterTag && projection.Register == carrier)
                .ToArray();
        }

        private static (TerminalCallTransfer Call, TerminalProjection[] Projections)? AllocateTerminalProjections(
            TerminalCallTransfer call,
            VirtualOperand[] arguments,
            int[] savedValues,
            int carrier,
            TerminalField[] fields
        )
        {
            var moves = call.Moves.ToList();
            var saved = call.SavedInputs!.ToList();
            var projections = new List<TerminalProjection>();
            foreach (TerminalField field in fields)
            {
                int[] destinations = Enumerable.Range(0, arguments.Length)
                    .Where(index => arguments[index].Value == field.Value).ToArray();
                int slot = Array.IndexOf(savedValues, field.Value);
                if (slot >= 0)
                {
                    projections.Add(new(
                        carrier,
                        YRegisterTag,
                        slot,
                        field.Opcode,
                        field.Index
                    ));
                    foreach (int destination in destinations)
                    {
                        saved.Add((destination, slot));
                    }

                    moves.RemoveAll(move => move.Source == field.Value);
                }
                else if (destinations.Length > 0)
                {
                    if (destinations is not [int destination]
                        || destination != field.Value && arguments.Any(argument => argument.Value == destination))
                    {
                        return null;
                    }

                    projections.Add(new(
                        carrier,
                        XRegisterTag,
                        destination,
                        field.Opcode,
                        field.Index
                    ));
                    moves.RemoveAll(move => move.Source == field.Value);
                }
            }

            return (call with { Moves = moves, SavedInputs = saved }, projections
                .OrderBy(projection => projection.Tag == XRegisterTag && projection.Register == carrier)
                .ToArray());
        }

        private void EmitTerminalFrame(TerminalCaseBranch branch, int heapWords = 0)
        {
            EmitFrameAllocation(branch.FrameLive, branch.EntrySpills.Length, heapWords);
            int[] laterSlots = Enumerable.Range(0, branch.EntrySpills.Length)
                .Where(slot => branch.EntrySpills[slot] < 0).ToArray();
            if (laterSlots.Length > 0)
            {
                owner.instructions.WriteByte(InitYRegisters);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, laterSlots.Length);
                foreach (int slot in laterSlots)
                {
                    owner.Operand(YRegisterTag, slot);
                }
            }

            var projectedSlots = branch.Projections.Where(projection => projection.Tag == YRegisterTag)
                .Select(projection => projection.Register).ToHashSet();
            for (int slot = 0; slot < branch.EntrySpills.Length; slot++)
            {
                if (projectedSlots.Contains(slot) || branch.EntrySpills[slot] < 0)
                {
                    continue;
                }

                owner.MoveRegister(
                    XRegisterTag,
                    branch.EntrySpills[slot],
                    YRegisterTag,
                    slot
                );
            }
        }

        private void EmitTerminalCaseArm(
            TerminalCaseBranch branch,
            bool frame,
            bool frameAlreadyOpen = false,
            BeamSourceLocation? startLine = null
        )
        {
            int reservedHeap = 0;
            if (frame && !frameAlreadyOpen)
            {
                reservedHeap = branch.Calls.FirstOrDefault()?.Constructions?.HeapWords ?? 0;
                EmitTerminalFrame(branch, reservedHeap);
            }

            EmitTerminalProjections(branch.Projections);
            BeamSourceLocation previous = startLine ?? location;
            foreach (AllocatedTerminalCall call in branch.Calls)
            {
                if (call.HeapLiveBeforeProjections is int live && call.Constructions is { HeapWords: > 0 } region)
                {
                    owner.Op(TestHeap, BeamWire.UnsignedTag, region.HeapWords);
                    owner.Operand(BeamWire.UnsignedTag, live);
                    reservedHeap = region.HeapWords;
                }

                EmitTerminalProjections(call.Projections);
                if (call.Constructions is { } prefix)
                {
                    EmitTerminalConstructions(prefix, heapReserved: reservedHeap != 0);
                    reservedHeap = 0;
                }

                TerminalCallTransfer entry = call.Entry;
                EmitTerminalCallOperands(call);
                if (entry.Source is Expr.Apply apply)
                {
                    owner.EmitArithmeticLine(apply.SourceLine, ref previous);
                    owner.Op(CallFun, BeamWire.UnsignedTag, apply.Arguments.Count);
                    continue;
                }

                var direct = (Expr.Call)entry.Source;
                MarkPlannedMapDefault(direct, entry.Module);
                if (!call.Tail || entry.Module is not null)
                {
                    owner.EmitCallLine(direct, previous);
                    previous = CallLocation(direct, previous);
                }

                if (call.Tail)
                {
                    EmitTerminalCallTail(
                        direct,
                        entry.Module,
                        frame,
                        call.RemainingSlots
                    );

                    return;
                }

                Call(entry.Module, direct.Function, direct.Arguments.Count);

            }

            bool returnHeapReserved = branch.ReturnHeapLiveBeforeProjections is not null
                && branch.ReturnConstructions.HeapWords > 0;
            if (returnHeapReserved)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, branch.ReturnConstructions.HeapWords);
                owner.Operand(BeamWire.UnsignedTag, branch.ReturnHeapLiveBeforeProjections!.Value);
            }

            EmitTerminalProjections(branch.ReturnProjections);
            var values = EmitTerminalConstructions(branch.ReturnConstructions, heapReserved: returnHeapReserved);
            if (branch.Result.Value is int result)
            {
                var (tag, register) = values[result];
                owner.MoveToX0(tag, register);
            }
            else
            {
                owner.EmitConstant(branch.Result.Literal!, 0);
            }

            if (frame)
            {
                owner.EmitFrameReturn(branch.RemainingSlots);
            }
            else
            {
                owner.instructions.WriteByte(Return);
            }

        }

        private Dictionary<int, (int Tag, int Register)> EmitTerminalConstructions(
            TerminalConstructionPlan plan,
            bool heapReserved = false
        )
        {
            var values = new Dictionary<int, (int Tag, int Register)>(plan.InitialValues);
            if (!heapReserved && plan.HeapWords > 0)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, plan.HeapWords);
                owner.Operand(BeamWire.UnsignedTag, plan.Live);
            }

            foreach (var (operation, register) in plan.Schedule)
            {
                if (operation.Cons)
                {
                    owner.instructions.WriteByte(PutList);
                    Write(operation.Inputs[0]);
                    Write(operation.Inputs[1]);
                    owner.Operand(XRegisterTag, register);
                }
                else
                {
                    owner.EmitTupleHeader(XRegisterTag, operation.Inputs.Length, register);
                    foreach (VirtualOperand input in operation.Inputs)
                    {
                        Write(input);
                    }
                }

                values.Add(operation.Result, (XRegisterTag, register));
            }

            return values;

            void Write(VirtualOperand operand)
            {
                if (operand.Value is int value)
                {
                    var (tag, register) = values[value];
                    owner.Operand(tag, register);
                }
                else
                {
                    owner.LiteralOperand(operand.Literal!);
                }
            }
        }

        private void EmitTerminalProjection(TerminalProjection projection)
        {
            owner.Op(projection.Opcode, projection.InputTag, projection.Input);
            if (projection.Opcode == GetTupleElement)
            {
                owner.Operand(BeamWire.UnsignedTag, projection.Index);
            }

            owner.Operand(projection.Tag, projection.Register);
        }

        private void EmitTerminalProjections(TerminalProjection[] projections)
        {
            if (projections is [var first, var second] && first.InputTag == XRegisterTag && second.InputTag == XRegisterTag
                && first.Input == second.Input
                && (first.Opcode, second.Opcode) is (GetHead, GetTail) or (GetTail, GetHead))
            {
                var (head, tail) = first.Opcode == GetHead ? (first, second) : (second, first);
                owner.EmitListParts(
                    head.Input,
                    head.Tag,
                    head.Register,
                    tail.Tag,
                    tail.Register
                );

                return;
            }

            foreach (TerminalProjection projection in projections)
            {
                EmitTerminalProjection(projection);
            }
        }

        private void EmitTerminalCallOperands(AllocatedTerminalCall call)
        {
            TerminalCallTransfer entry = call.Entry;
            if (call.DeferredArgumentSlot is int outgoing)
            {
                int scratch = entry.OperandCount;
                owner.MoveRegister(
                    YRegisterTag,
                    outgoing,
                    XRegisterTag,
                    scratch
                );
                owner.EmitOrderedTailMoves(entry.Moves, entry.Constants, entry.SavedInputs!
                    .Where(input => input.Destination != 0).ToList());
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    call.RetainedResultSlot!.Value
                );
                owner.Op(TrimFrame, BeamWire.UnsignedTag, call.Trim);
                owner.Operand(BeamWire.UnsignedTag, call.RemainingSlots);
                owner.MoveRegister(
                    XRegisterTag,
                    scratch,
                    XRegisterTag,
                    0
                );

                return;
            }

            if (call.RetainedResultSlot is not int resultSlot)
            {
                Term? finalConstant = call.Trim != 0 && (call.Projections.Length != 0 || call.TrimMoves.Length != 0)
                    ? entry.Constants.FirstOrDefault(move => move.Destination == 0).Value : null;
                owner.EmitOrderedTailMoves(
                    entry.Moves,
                    entry.Constants,
                    entry.SavedInputs,
                    deferX0Constant: finalConstant is not null
                );
                foreach (var (source, destination) in call.TrimMoves)
                {
                    owner.MoveRegister(
                        YRegisterTag,
                        source,
                        YRegisterTag,
                        destination
                    );
                }

                if (call.Trim != 0)
                {
                    owner.Op(TrimFrame, BeamWire.UnsignedTag, call.Trim);
                    owner.Operand(BeamWire.UnsignedTag, call.RemainingSlots);
                }

                if (finalConstant is not null)
                {
                    owner.EmitConstant(finalConstant, 0);
                }

                return;
            }

            if (!entry.SavedInputs!.Any(input => input.Source == resultSlot))
            {
                // Save the preceding result before outgoing X copies. If the
                // destination Y slot is also an input, copy its old value first.
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    resultSlot
                );
                owner.EmitOrderedTailMoves(entry.Moves, entry.Constants, entry.SavedInputs);

                return;
            }

            // Before argument construction, X0 holds the preceding result. Finish all
            // copies from that value and from its destination slot before the
            // result takes the slot. A reciprocal outgoing copy can be a swap.
            owner.EmitOrderedTailMoves(entry.Moves, []);
            var reads = entry.SavedInputs!;
            foreach (var (destination, source) in reads)
            {
                if (source == resultSlot && destination != 0)
                {
                    owner.MoveRegister(
                        YRegisterTag,
                        source,
                        XRegisterTag,
                        destination
                    );
                }
            }

            if (reads.Contains((0, resultSlot)))
            {
                owner.Op(Swap, YRegisterTag, resultSlot);
                owner.Operand(XRegisterTag, 0);
            }
            else
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    resultSlot
                );
            }

            owner.EmitOrderedTailMoves([], entry.Constants, reads.Where(read => read.Source != resultSlot).ToList());
        }

        private void EmitTerminalCallTail(
            Expr.Call call,
            string? module,
            bool frame,
            int slots
        )
        {
            int arity = call.Arguments.Count;
            if (module is null)
            {
                owner.Op(frame ? TailLocalCallWithFrame : TailLocalCall, BeamWire.UnsignedTag, arity);
                int plannedEntry = entries[(call.Function, arity)];
                owner.Operand(LabelTag, owner.LocalCallTarget(call.Function, arity, plannedEntry));
            }
            else
            {
                owner.Op(frame ? TailExternalCallWithFrame : TailExternalCall, BeamWire.UnsignedTag, arity);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(module, call.Function, arity));
            }

            if (frame)
            {
                owner.Operand(BeamWire.UnsignedTag, slots);
            }
        }

        private static IEnumerable<int> ReadValues(IEnumerable<VirtualOperand> operands) =>
            operands.Select(operand => operand.Value).OfType<int>();
    }
}
