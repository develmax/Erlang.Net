using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private const int SharedTupleFrame = -1;
        private const int TupleFallbackFrame = -2;
        internal IReadOnlyDictionary<int, int>? TupleFrames { get; private set; }

        private bool TryEmitGroupedTupleEntry(FunctionDefinition function, int functionInfo)
        {
            if (function.Arity == 0 || function.Clauses.Count < 2)
            {
                return false;
            }

            Clause? fallback = function.Clauses[^1] is { Guard: null } last && HasUnconditionalParameters(last)
                ? last : null;
            var tupleClauses = fallback is null ? function.Clauses : function.Clauses.SkipLast(1).ToArray();
            int input = Enumerable.Range(0, function.Arity).FirstOrDefault(index =>
                tupleClauses.All(clause => EntryShape(clause.Patterns[index]) is Pattern.Tuple), -1);
            (Atom Key, Clause Clause)[] atomArms = [];
            if (input < 0)
            {
                if (ReadGroupedMixedEntry(function, fallback, functionInfo) is not { } mixed)
                {
                    return false;
                }

                input = 0;
                tupleClauses = mixed.Tuples;
                atomArms = mixed.Atoms;
            }

            // Core partitions the common leading literal before the tuple column.
            Atom? prefix = input == 1 && tupleClauses[0].Patterns[0] is Pattern.Literal { Value: Atom leading }
                && tupleClauses.All(clause => clause.Patterns[0] is Pattern.Literal { Value: Atom value } && leading.Equals(value))
                ? leading : null;
            if (prefix is not null)
            {
                tupleClauses = tupleClauses.Select(clause =>
                    clause with { Patterns = [new Pattern.Any(), .. clause.Patterns.Skip(1)] }).ToArray();
            }

            var groups = tupleClauses
                .GroupBy(clause => ((Pattern.Tuple)EntryShape(clause.Patterns[input])).Items.Count)
                .OrderBy(group => group.Key).ToArray();
            if (groups.Length < 2)
            {
                return false;
            }

            if (input == 0 && atomArms.Length == 0 && (TryEmitGroupedTupleProjections(
                function,
                groups,
                fallback,
                functionInfo
            )
                || fallback is not null && TryEmitTupleGroupsWithDynamicCase(groups, fallback)))
            {
                return true;
            }

            bool useEntryScratch = fallback is not null || input != 0;
            var cases = new (int Arity, int Label)[groups.Length];
            for (int index = groups.Length - 1; index >= 0; index--)
            {
                cases[index] = (groups[index].Key, Label());
            }

            int noMatch = fallback is null ? functionInfo : Label();
            int? nonTuple = atomArms.Length == 0 ? null : Label();
            if (prefix is not null)
            {
                owner.Op(ExactEqualTest, LabelTag, noMatch);
                owner.Operand(XRegisterTag, 0);
                owner.LiteralOperand(prefix);
            }

            owner.EmitTupleArityDispatch(
                noMatch,
                cases,
                input,
                typeFailure: nonTuple
            );

            int argumentBase = nextSlot;
            int[] arguments = Enumerable.Range(0, function.Arity).Select(_ => Slot()).ToArray();
            int clauseBase = nextSlot;
            int? failed = null;
            int functionFrame = frameSize;
            int functionPeak = peakSlot;
            var frames = new Dictionary<int, int>();
            var recordFailures = new List<(int Label, BeamSourceLocation Location)>();
            int nextNestedFrameKey = TupleFallbackFrame - 1;
            for (int groupIndex = groups.Length - 1; groupIndex >= 0; groupIndex--)
            {
                Mark(cases[groupIndex].Label);
                var group = groups[groupIndex];
                var clauses = group.ToArray();
                peakSlot = argumentBase;
                frameSize = plannedTupleFrames?.GetValueOrDefault(group.Key, functionFrame) ?? functionFrame;
                if (clauses is [var checkedTuple] && TryEmitGroupedTupleNilCase(
                    checkedTuple,
                    input,
                    noMatch
                ))
                {
                    frames.Add(group.Key, peakSlot);
                    functionPeak = Math.Max(functionPeak, peakSlot);
                    continue;
                }

                if (clauses is [var single] && (TryEmitGroupedRecordFieldGuardReturn(single, input, noMatch)
                    || useEntryScratch && TryEmitTupleMapEntry(
                        single,
                        input,
                        noMatch,
                        argumentBase
                    ) || TryEmitTupleEntryWithRegisterFields(
                        single,
                        noMatch,
                        argumentBase,
                        groupedInput: useEntryScratch ? input : null
                    )))
                {
                    frames.Add(group.Key, peakSlot);
                    functionPeak = Math.Max(functionPeak, peakSlot);
                    continue;
                }

                if (clauses is [var recordCarrier] && TryEmitGroupedRecordCarrierEntry(
                    recordCarrier,
                    input,
                    noMatch,
                    argumentBase
                ))
                {
                    frames.Add(group.Key, peakSlot);
                    functionPeak = Math.Max(functionPeak, peakSlot);
                    continue;
                }

                if (argumentBase == 0 && TryEmitSharedTaggedConsEntry(
                    clauses,
                    input,
                    fallback,
                    noMatch
                ))
                {
                    frames.Add(group.Key, peakSlot);
                    functionPeak = Math.Max(functionPeak, peakSlot);
                    continue;
                }

                if (argumentBase == 0 && (TryEmitFlatTaggedTupleGroup(
                    clauses,
                    input,
                    noMatch,
                    frames,
                    recordFailures,
                    ref nextNestedFrameKey
                ) || TryEmitNestedTupleGroup(
                    clauses,
                    input,
                    noMatch,
                    frames,
                    recordFailures,
                    ref nextNestedFrameKey
                ) || !owner.IsDeclaredNative((function.Name, function.Arity)) && TryEmitTaggedStateTupleGroup(
                    clauses,
                    input,
                    noMatch,
                    frames,
                    recordFailures,
                    ref nextNestedFrameKey
                )))
                {
                    owner.nestedEntryLabelOrder.Add((function.Name, function.Arity));
                    functionPeak = Math.Max(functionPeak, peakSlot);
                    nextSlot = clauseBase;
                    continue;
                }

                peakSlot = clauseBase;
                frameSize = plannedTupleFrames?.GetValueOrDefault(SharedTupleFrame, functionFrame) ?? functionFrame;
                failed ??= Label();
                EmitFrameAllocation(function.Arity, frameSize);
                EmitFrameInitialization(clauseBase - argumentBase, frameSize);
                for (int index = 0; index < arguments.Length; index++)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        index,
                        YRegisterTag,
                        arguments[index]
                    );
                }

                // The mixed entry's tuple paths share emitted frames, but not matched values.
                bool rememberRecords = atomArms.Length != 0;
                var previousRecords = checkedRecordBindings;
                for (int clauseIndex = 0; clauseIndex < clauses.Length; clauseIndex++)
                {
                    Clause clause = clauses[clauseIndex];
                    int failure = clauseIndex == clauses.Length - 1 ? failed.Value : Label();
                    nextSlot = clauseBase;
                    variables = new(StringComparer.Ordinal);
                    if (rememberRecords)
                    {
                        checkedRecordBindings = new(StringComparer.Ordinal);
                    }

                    for (int index = 0; index < arguments.Length; index++)
                    {
                        Pattern(
                            clause.Patterns[index],
                            arguments[index],
                            failure,
                            checkedShape: index == input ? EntryShape(clause.Patterns[index]) : null,
                            checkedTupleTag: false,
                            entryPatternScratch: useEntryScratch
                        );
                    }

                    Guard(clause.Guard, failure, formAtomSelection: false);
                    if (rememberRecords)
                    {
                        foreach (Pattern pattern in clause.Patterns)
                        {
                            RememberEntryPatternRecords(pattern, includeLists: true);
                        }
                    }

                    if (!TailLocal(clause.Body))
                    {
                        Expression(clause.Body);
                        owner.EmitFrameReturn(frameSize);
                    }

                    if (rememberRecords)
                    {
                        checkedRecordBindings = previousRecords;
                    }

                    if (failure != failed)
                    {
                        Mark(failure);
                    }
                }

                frames[SharedTupleFrame] = Math.Max(frames.GetValueOrDefault(SharedTupleFrame), peakSlot);
                functionPeak = Math.Max(functionPeak, peakSlot);
            }

            TupleFrames = frames;
            peakSlot = functionPeak;
            frameSize = functionFrame;
            if (failed is int failureLabel)
            {
                Mark(failureLabel);
                for (int index = 0; index < arguments.Length; index++)
                {
                    Load(arguments[index], index);
                }

                owner.Op(
                    Deallocate,
                    BeamWire.UnsignedTag,
                    plannedTupleFrames?.GetValueOrDefault(SharedTupleFrame, functionFrame) ?? functionFrame
                );
                if (fallback is null || nonTuple.HasValue)
                {
                    JumpTo(noMatch);
                }
            }

            var atomFailures = new List<EntryEffectFailure>();
            if (nonTuple is int atomEntry)
            {
                EmitAtoms(atomEntry);
            }

            if (fallback is not null)
            {
                Mark(noMatch);
                EmitGroupedTupleFallback(fallback, frames);
            }

            // The short record-error exits follow the ordinary entry arms in beam_jump.
            foreach (var (label, at) in recordFailures)
            {
                EmitCheckedResultRecordFailure(label, at);
            }

            foreach (var failure in atomFailures)
            {
                Mark(failure.Label);
                if (failure.Line is { } line)
                {
                    owner.EmitLine(line);
                }

                owner.Op(CaseEnd, XRegisterTag, failure.Register);
            }

            return true;

            void EmitAtoms(int atomEntry)
            {
                Mark(atomEntry);
                int[] labels = atomArms.Length == 1 ? [] : atomArms.Select(_ => Label()).Reverse().ToArray();
                if (labels.Length == 0)
                {
                    owner.Op(ExactEqualTest, LabelTag, noMatch);
                    owner.Operand(XRegisterTag, 0);
                    owner.LiteralOperand(atomArms[0].Key);
                }
                else
                {
                    owner.EmitMembershipSelect(0, noMatch,
                        atomArms.Select((arm, index) => ((Term)arm.Key, labels[index])));
                }

                var context = new SharedNestedEntryContext(
                    null,
                    null,
                    noMatch,
                    Failures: atomFailures,
                    CheckedAtomArgument: 0,
                    AllowConsTupleField: true
                );
                var originalVariables = variables;
                for (int index = atomArms.Length - 1; index >= 0; index--)
                {
                    if (labels.Length != 0)
                    {
                        Mark(labels[index]);
                    }

                    int frameKey = nextNestedFrameKey--;
                    Clause clause = atomArms[index].Clause;
                    var checkedClause = OmitGroupedEntryAtom(clause);
                    if (HasUnconditionalParameters(checkedClause))
                    {
                        EmitGroupedTupleFallback(checkedClause, frames, frameKey);
                        continue;
                    }

                    int previousSlot = nextSlot;
                    int previousPeak = peakSlot;
                    nextSlot = argumentBase;
                    peakSlot = argumentBase;
                    variables = new(originalVariables, StringComparer.Ordinal);
                    frameSize = plannedTupleFrames?.GetValueOrDefault(frameKey, functionFrame) ?? functionFrame;
                    TryEmitPlannedTupleEntry(clause, noMatch, sharedEntry: context);
                    frames.Add(frameKey, peakSlot);
                    peakSlot = Math.Max(previousPeak, peakSlot);
                    nextSlot = previousSlot;
                    frameSize = functionFrame;
                    variables = originalVariables;
                }
            }
        }

        private (Clause[] Tuples, (Atom Key, Clause Clause)[] Atoms)? ReadGroupedMixedEntry(
            FunctionDefinition function,
            Clause? fallback,
            int failure
        )
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || fallback is null
                || function.Clauses.Any(clause => LocalFunctionEntries.Walk(clause.Body).Any(expression =>
                    expression is Expr.Try or Expr.Receive or Expr.Catch or Expr.Maybe or Expr.Fun)))
            {
                return null;
            }

            var context = new SharedNestedEntryContext(
                null,
                null,
                failure,
                CheckedAtomArgument: 0,
                AllowConsTupleField: true
            );
            var arities = new HashSet<int>();
            var keys = new HashSet<Atom>();
            var tuples = new List<Clause>();
            var atoms = new List<(Atom Key, Clause Clause)>();
            foreach (Clause clause in function.Clauses.SkipLast(1))
            {
                if (EntryShape(clause.Patterns[0]) is Pattern.Tuple tuple && HasTupleTag(tuple, out _))
                {
                    arities.Add(tuple.Items.Count);
                    tuples.Add(clause);
                    continue;
                }

                if (clause is not { Guard: null, Patterns: [Pattern.Literal { Value: Atom key }, ..] }
                    || !keys.Add(key)
                    || !HasUnconditionalParameters(OmitGroupedEntryAtom(clause))
                        && !TryEmitPlannedTupleEntry(
                            clause,
                            failure,
                            validateOnly: true,
                            sharedEntry: context
                        ))
                {
                    return null;
                }

                atoms.Add((key, clause));
            }

            return arities.Count >= 3 && atoms.Count > 0
                ? (tuples.ToArray(), atoms.OrderBy(arm => arm.Key, Comparer<Atom>.Create(CompareAtoms)).ToArray())
                : null;
        }

        private static Clause OmitGroupedEntryAtom(Clause clause) => clause with
        {
            Patterns = [new Pattern.Variable(VariableScopeNames.Wildcard), .. clause.Patterns.Skip(1)]
        };

        private void EmitGroupedTupleFallback(
            Clause clause,
            Dictionary<int, int> frames,
            int frameKey = TupleFallbackFrame
        )
        {
            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (AttributeLiteral.TryNormalizeConstant(clause.Body, out Term constant))
            {
                owner.EmitConstant(constant, 0);
                owner.instructions.WriteByte(Return);

                return;
            }

            if (clause.Body is Expr.Variable { Name: not VariableScopeNames.Wildcard } variable
                && Array.IndexOf(parameters, variable.Name) is >= 0 and var result)
            {
                if (result != 0)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        result,
                        XRegisterTag,
                        0
                    );
                }

                owner.instructions.WriteByte(Return);

                return;
            }

            if (clause.Body is Expr.Call call && ReadEntryCall(call, parameters) is { } tail)
            {
                owner.EmitOrderedTailMoves(tail.Moves, tail.Constants);
                EmitRegisterTailTarget(tail.Call with { Module = tail.Module }, tail.Call.Arguments.Count);

                return;
            }

            if (!owner.ModuleOptimizationDisabled)
            {
                var checkedEntry = parameters.Select((name, register) => (name, register))
                    .Where(input => input.name != VariableScopeNames.Wildcard)
                    .ToDictionary(input => input.name, input => (input.register, (int?)null), StringComparer.Ordinal);
                if (TryEmitSingleEntryEffect(clause, checkedEntry))
                {
                    return;
                }
            }

            var used = ReadUsedEntryParameters(clause);
            var inputs = parameters.Select((name, register) => (Variable: new Pattern.Variable(name), Register: register))
                .Where(binding => binding.Variable.Name != VariableScopeNames.Wildcard
                    && (used is null || used.Contains(binding.Variable.Name))).ToArray();
            EmitEntryClauseBody(
                clause,
                inputs,
                frameKey,
                frames,
                reusedTail: null,
                deferred: null,
                literalDispatch: false
            );
        }

        private bool TryEmitConstructedEntryWithFallback(FunctionDefinition function)
        {
            if (function.Clauses is not [var first, { Guard: null } fallback] || function.Arity == 0
                || !HasUnconditionalParameters(fallback))
            {
                return false;
            }

            var parameters = fallback.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            int result = -1;
            Term? constant = null;
            EntryCall? tail = null;
            List<(int Register, Term? Literal)>? tuple = null;
            if (fallback.Body is Expr.Variable { Name: not VariableScopeNames.Wildcard } variable)
            {
                result = Array.IndexOf(parameters, variable.Name);
                if (result < 0)
                {
                    return false;
                }
            }
            else if (fallback.Body is Expr.Call call)
            {
                tail = ReadEntryCall(call, parameters);
                if (tail is null)
                {
                    return false;
                }
            }
            else if (fallback.Body is Expr.Tuple construction && construction.Items.Any(item => item is Expr.Variable))
            {
                tuple = [];
                foreach (Expr item in construction.Items)
                {
                    if (item is Expr.Variable field && Array.IndexOf(parameters, field.Name) is int register && register >= 0)
                    {
                        tuple.Add((register, null));
                    }
                    else if (AttributeLiteral.TryNormalizeConstant(item, out Term literal))
                    {
                        tuple.Add((-1, literal));
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else if (!AttributeLiteral.TryNormalizeConstant(fallback.Body, out constant))
            {
                return false;
            }

            int failure = owner.nextLabel;
            bool emitted = first.Patterns[0] switch
            {
                Pattern.Tuple { } => TryEmitTupleEntryWithRegisterFields(
                    first,
                    failure,
                    baseSlot: null,
                    reserveFailure: ReserveFailure
                )
                    || TryEmitTupleEntryWithRegisterFields(
                        first,
                        failure,
                        nextSlot,
                        ReserveFailure,
                        checkShape: true
                    )
                    || TryEmitPlannedTupleEntry(first, failure, ReserveFailure),
                Pattern.List { } when first.Body is Expr.Call { DynamicRemote: false } =>
                    TryEmitConsEntryInRegisters(
                        first,
                        0,
                        failure,
                        EmitListTest
                    ),
                _ => false
            };
            if (!emitted)
            {
                return false;
            }

            Mark(failure);
            if (tail is not null)
            {
                owner.EmitArgumentMoves(tail.Moves, tail.Constants);
                EmitRegisterTailTarget(tail.Call with { Module = tail.Module }, tail.Call.Arguments.Count);
            }
            else
            {
                if (tuple is not null)
                {
                    owner.Op(TestHeap, BeamWire.UnsignedTag, tuple.Count + 1);
                    owner.Operand(BeamWire.UnsignedTag, tuple.Max(field => field.Register) + 1);
                    owner.Op(PutTuple, XRegisterTag, 0);
                    owner.instructions.WriteByte(ExtendedListOperand);
                    owner.Operand(BeamWire.UnsignedTag, tuple.Count);
                    foreach (var (register, literal) in tuple)
                    {
                        if (literal is null)
                        {
                            owner.Operand(XRegisterTag, register);
                        }
                        else
                        {
                            owner.LiteralOperand(literal);
                        }
                    }
                }
                else if (constant is not null)
                {
                    owner.EmitConstant(constant, 0);
                }
                else if (result != 0)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        result,
                        XRegisterTag,
                        0
                    );
                }

                owner.instructions.WriteByte(Return);
            }

            return true;

            void ReserveFailure() => Label();

            void EmitListTest()
            {
                ReserveFailure();
                owner.EmitNonemptyList(0, failure);
            }
        }

        private bool TryEmitDeferredRecordList(Clause clause, int failure)
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple record, Pattern.Variable saved],
                    Body: Expr.Sequence
                    {
                        Items: [
                            Expr.Call first,
                            Expr.Call { Arguments: [var prefix, Expr.List { Tail: null } list] } second,
                            Expr.Tuple { Items: [Expr.Literal result, Expr.Variable returned] }
                        ]
                    }
                } || returned.Name != saved.Name || !HasTupleTag(record, out Atom tag)
                || record.Items.Skip(1).Any(item => item is not Pattern.Variable { })
                || list.Items.Count == 0 || list.Items.Count >= FirstReservedXRegister
                || ReadEntryCall(first, []) is not { } firstCall
                || !TryResolveOrdinaryCall(second, out string? module, preparedArgument: 1)
                || !AttributeLiteral.TryNormalizeConstant(prefix, out Term constant))
            {
                return false;
            }

            var names = record.Items.Skip(1).Cast<Pattern.Variable>().Select(field => field.Name).ToArray();
            if (saved.Name == VariableScopeNames.Wildcard || names.Contains(saved.Name, StringComparer.Ordinal)
                || names.Where(name => name != VariableScopeNames.Wildcard).Distinct(StringComparer.Ordinal).Count()
                    != names.Count(name => name != VariableScopeNames.Wildcard))
            {
                return false;
            }

            var fields = new int[list.Items.Count];
            for (int index = 0; index < fields.Length; index++)
            {
                if (list.Items[index] is not Expr.Variable variable || variable.Name == VariableScopeNames.Wildcard
                    || (fields[index] = Array.IndexOf(names, variable.Name) + 1) == 0)
                {
                    return false;
                }
            }

            EmitDetailRecordTest(
                0,
                record.Items.Count,
                tag,
                failure
            );
            EmitFrameAllocation(2, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );
            EmitEntryCall(firstCall);
            owner.Op(TestHeap, BeamWire.UnsignedTag, fields.Length * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 0);
            for (int index = 0; index < fields.Length; index++)
            {
                owner.Op(GetTupleElement, YRegisterTag, 1);
                owner.Operand(BeamWire.UnsignedTag, fields[index]);
                owner.Operand(XRegisterTag, index);
            }

            for (int index = fields.Length - 1; index >= 0; index--)
            {
                owner.Op(PutList, XRegisterTag, index);
                if (index == fields.Length - 1)
                {
                    owner.LiteralOperand(Nil.Value);
                }
                else
                {
                    owner.Operand(XRegisterTag, index + 1);
                }

                owner.Operand(XRegisterTag, Math.Max(index, 1));
            }

            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitConstant(constant, 0);
            owner.EmitCallLine(second, CallLocation(first, location));
            Call(module, second.Function, 2);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.LiteralOperand(result.Value);
            owner.Operand(YRegisterTag, 0);
            owner.EmitFrameReturn(1);

            return true;
        }

        private bool TryEmitSharedTaggedTupleDispatch(FunctionDefinition function, int failure)
        {
            if (function.Arity == 0 || function.Clauses is not [var left, var right]
                || left.Patterns[0] is not Pattern.Tuple first || right.Patterns[0] is not Pattern.Tuple second
                || first.Items.Count != second.Items.Count
                || !HasTupleTag(first, out Atom tag) || !HasTupleTag(second, out Atom otherTag)
                || !tag.Equals(otherTag))
            {
                return false;
            }

            int selector = -1;
            Atom? leftKey = null;
            Atom? rightKey = null;
            var sharedFields = new HashSet<int>();
            for (int index = 1; index < first.Items.Count; index++)
            {
                if (first.Items[index] is not Pattern.Literal a || second.Items[index] is not Pattern.Literal b)
                {
                    continue;
                }

                if (a.Value.Equals(b.Value))
                {
                    sharedFields.Add(index);
                }
                else if (selector < 0 && a.Value is Atom firstKey && b.Value is Atom secondKey)
                {
                    selector = index;
                    leftKey = firstKey;
                    rightKey = secondKey;
                }
            }

            if (selector < 0)
            {
                return false;
            }

            sharedFields.Add(selector);
            if (!TryEmitPlannedTupleEntry(
                left,
                failure,
                checkedRootFields: sharedFields,
                validateOnly: true
            )
                || !TryEmitPlannedTupleEntry(
                    right,
                    failure,
                    checkedRootFields: sharedFields,
                    validateOnly: true
                ))
            {
                return false;
            }

            if (TermOrder.Compare(leftKey!, rightKey!, exact: true) > 0)
            {
                (leftKey, rightKey) = (rightKey, leftKey);
                (left, right) = (right, left);
            }

            int register = function.Arity;
            EmitDetailRecordTest(
                0,
                first.Items.Count,
                tag,
                failure
            );
            foreach (int field in sharedFields.Order())
            {
                if (field == selector)
                {
                    continue;
                }

                owner.EmitTuplePart(0, field, register);
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.Operand(XRegisterTag, register);
                owner.LiteralOperand(((Pattern.Literal)first.Items[field]).Value);
            }

            int rightLabel = Label();
            int leftLabel = Label();
            owner.EmitTuplePart(0, selector, register);
            owner.EmitAtomSelection(
                register,
                failure,
                leftKey!.Name,
                leftLabel,
                rightKey!.Name,
                rightLabel
            );
            int slotBase = nextSlot;
            Mark(rightLabel);
            TryEmitPlannedTupleEntry(right, failure, checkedRootFields: sharedFields);
            nextSlot = slotBase;
            Mark(leftLabel);

            return TryEmitPlannedTupleEntry(left, failure, checkedRootFields: sharedFields);
        }

        private bool TryEmitTupleEntryCallSequence(Clause clause, int failure)
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple tuple],
                    Body: Expr.Sequence { Items: [Expr.Call firstCall, _, ..] } sequence
                }
                || tuple.Items.Count >= FirstReservedXRegister
                || tuple.Items.Any(field => field is not Erlang.Pattern.Variable))
            {
                return false;
            }

            var fields = tuple.Items.Cast<Pattern.Variable>().ToArray();
            if (!DistinctBindings(fields))
            {
                return false;
            }

            string[] parameters = [VariableScopeNames.Wildcard, .. fields.Select(field => field.Name)];
            if (ReadEntryCall(firstCall, parameters) is not { } first)
            {
                return false;
            }

            var continuation = new List<(EntryCall Call, int? ListDestination)>();
            for (int index = sequence.Items.Count - 1; index > 0; index--)
            {
                if (sequence.Items[index] is not Expr.Call call
                    || ReadEntryCall(call, [], parameters) is not { } step)
                {
                    return false;
                }

                continuation.Add((step, null));
            }

            EmitTupleShapeTest(
                XRegisterTag,
                0,
                fields.Length,
                failure
            );
            EmitEntryCallChain(
                first,
                parameters,
                continuation,
                relocateBeforeTrim: true,
                entryTupleArity: fields.Length
            );

            return true;
        }

        private bool TryEmitPlannedTupleEntry(
            Clause clause,
            int failure,
            Action? reserveFailure = null,
            HashSet<int>? checkedRootFields = null,
            bool validateOnly = false,
            SharedNestedEntryContext? sharedEntry = null,
            bool requireSingleEffect = false
        )
        {
            if (requireSingleEffect && (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || clause.Guard is not null || clause.Patterns[0] is not Pattern.Tuple primary
                || !primary.Items.Any(item => item is Pattern.Tuple)))
            {
                return false;
            }

            if (sharedEntry?.CheckedAtomArgument is null
                && ((sharedEntry is not null ? EntryShape(clause.Patterns[0]) : clause.Patterns[0]) is not Pattern.Tuple root
                || reserveFailure is not null && checkedRootFields is null && clause.Guard is null && !root.Items.Any(item => item is Pattern.Tuple)))
            {
                return false;
            }

            var sharedNested = sharedEntry?.Nested;
            int arity = clause.Patterns.Count;
            int nextRegister = sharedNested is null ? arity
                : Math.Max(sharedNested.Register, sharedNested.Projections.Values.DefaultIfEmpty(-1).Max()) + 1;
            if (sharedEntry?.ProjectedRootFields is { Count: > 0 } projectedRoot)
            {
                nextRegister = Math.Max(nextRegister, projectedRoot.Values.Max() + 1);
            }

            int? selector = sharedEntry?.ConsumedSelectorRegister;
            int? freeSecondaryProjection = selector;
            bool orderConstructorFirst = selector is not null || requireSingleEffect;
            var secondaryChecks = new List<Action>();
            var checks = secondaryChecks;
            var bindings = new Dictionary<string, (int Input, int? Field)>(StringComparer.Ordinal);
            List<Pattern>? checkedConsHeads = null;
            for (int index = sharedEntry?.CheckedAtomArgument is null ? 1 : 0; index < arity; index++)
            {
                if (index == sharedEntry?.CheckedAtomArgument)
                {
                    continue;
                }

                if (!(sharedEntry is not null || requireSingleEffect ? PlanArgument(clause.Patterns[index], index)
                    : clause.Patterns[index] is Pattern.Variable parameter && AddBinding(parameter, index, null)))
                {
                    return false;
                }
            }

            if (orderConstructorFirst)
            {
                // Constructor columns precede the remaining arguments in OTP match_clause.
                checks = [];
            }

            if (sharedEntry?.CheckedAtomArgument is null && !PlanArgument(clause.Patterns[0], 0))
            {
                return false;
            }

            if (orderConstructorFirst)
            {
                checks.AddRange(secondaryChecks);
            }

            var parameters = bindings.Keys.Select(name => new Pattern.Variable(name)).ToArray();
            var used = ReadUsedEntryParameters(new Clause(parameters, null, clause.Body));
            if (!PlanGuard(clause.Guard) || nextRegister >= FirstReservedXRegister)
            {
                return false;
            }

            if (validateOnly)
            {
                return true;
            }

            if (requireSingleEffect)
            {
                return TryEmitSingleEntryEffect(clause, bindings, EmitChecks);
            }

            EmitChecks();
            if (sharedEntry?.Failures is { } effectFailures && (TryEmitFiniteEntryEffects(clause.Body, bindings, effectFailures)
                || TryEmitSelectedEntryEffectResult(clause, bindings, effectFailures)
                || TryEmitSingleEntryEffect(clause, bindings) || TryEmitLinearEntryEffects(clause, bindings)))
            {
                return true;
            }
            if (sharedEntry is not null && used is not null
                && ReadDeferredEntryRecordFields(clause, bindings, parameters) is { } deferred)
            {
                clause = clause with { Body = deferred };
                used = ReadUsedEntryParameters(new Clause(parameters, null, deferred));
            }

            var saved = bindings.Where(binding => used is null || used.Contains(binding.Key)).ToArray();
            var initialProjection = ReadInitialEntryProjection(clause.Body, bindings, parameters);
            if (initialProjection is { } projection)
            {
                saved = saved.Where(binding => binding.Key != projection.Name).ToArray();
            }

            Expr.Call? forwardedCall = null;
            (int Input, int Field) forwardedField = default;
            (int? Slot, Term? Constant)[]? forwardedOperands = null;
            if (clause.Body is Expr.Sequence
                {
                    Items: [
                Expr.Match
                    {
                        Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } result,
                        Value: Expr.Call
                        {
                            Module: not null and not ErlangModuleNames.Module, DynamicRemote: false,
                            Arguments: [Expr.Variable argument]
                        } call
                    },
                Expr.Tuple resultTuple
            ]
                } && !bindings.ContainsKey(result.Name)
                && bindings.TryGetValue(argument.Name, out var source) && source.Field is int fieldIndex)
            {
                var surviving = saved.Where(binding => binding.Key != argument.Name).ToArray();
                var slots = surviving.Select((binding, index) => (binding.Key, Slot: nextSlot + surviving.Length - 1 - index))
                    .ToDictionary(binding => binding.Key, binding => binding.Slot, StringComparer.Ordinal);
                if (TryReadBoundTupleOperands(
                    resultTuple,
                    result.Name,
                    slots,
                    out var operands
                ))
                {
                    saved = surviving;
                    forwardedCall = call;
                    forwardedField = (source.Input, fieldIndex);
                    forwardedOperands = operands;
                }
            }
            EmitFrameAllocation(nextRegister, frameSize);
            EmitFrameInitialization(saved.Length, frameSize);
            variables = new(StringComparer.Ordinal);
            int bindingBase = nextSlot;
            var incomingRecords = checkedRecordBindings;

            foreach (var (name, (input, field)) in saved)
            {
                int slot = bindingBase + saved.Length - 1 - (Slot() - bindingBase);
                if (field is int position)
                {
                    owner.Op(GetTupleElement, XRegisterTag, input);
                    owner.Operand(BeamWire.UnsignedTag, position);
                    owner.Operand(YRegisterTag, slot);
                }
                else
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        input,
                        YRegisterTag,
                        slot
                    );
                }

                variables.Add(name, slot);
            }

            if (sharedEntry is not null)
            {
                checkedRecordBindings = new(checkedRecordBindings, StringComparer.Ordinal);
                foreach (var pattern in clause.Patterns)
                {
                    RememberEntryPatternRecords(pattern);
                }

                if (checkedConsHeads is { } heads)
                {
                    foreach (Pattern head in heads)
                    {
                        RememberEntryPatternRecords(head);
                    }
                }
            }

            if (forwardedCall is not null)
            {
                owner.EmitTuplePart(forwardedField.Input, forwardedField.Field, 0);
                owner.EmitCallLine(forwardedCall, location);
                Call(forwardedCall.Module, forwardedCall.Function, 1);
                EmitTupleWithX0(forwardedOperands!);
                owner.EmitFrameReturn(frameSize);
            }
            else if (initialProjection is { } initial)
            {
                owner.EmitTuplePart(initial.Input, initial.Field, 0);
                owner.EmitCallLine(initial.Call, location);
                Call(initial.Module, initial.Call.Function, 1);
                Expression(initial.Continuation);
                owner.EmitFrameReturn(frameSize);
            }
            else if (!TailLocal(clause.Body))
            {
                if (clause.Body is not Expr.Sequence { Items: [var resultBinding, var resultValue] }
                    || !TryEmitBoundCallTuple(resultBinding, resultValue))
                {
                    Expression(clause.Body);
                }
                owner.EmitFrameReturn(frameSize);
            }

            checkedRecordBindings = incomingRecords;

            return true;

            void EmitChecks()
            {
                reserveFailure?.Invoke();
                foreach (Action check in checks)
                {
                    check();
                }
            }

            bool PlanArgument(Pattern pattern, int input)
            {
                return pattern switch
                {
                    Pattern.Variable variable => AddBinding(variable, input, null),
                    Pattern.Tuple tuple => PlanTuple(tuple, input),
                    AliasPattern alias when TryReadPatternAlias(alias, out var variable, out var shape)
                        && shape is Pattern.Tuple tuple && AddBinding(variable, input, null) => PlanTuple(tuple, input),
                    _ => false
                };
            }

            bool AddBinding(Pattern.Variable variable, int input, int? field) =>
                variable.Name == VariableScopeNames.Wildcard || bindings.TryAdd(variable.Name, (input, field));

            bool PlanGuard(Expr? guard)
            {
                if (guard is null)
                {
                    return true;
                }

                if (guard is Expr.Binary { Operator: ErlangOperators.AndAlso } conjunction)
                {
                    return PlanGuard(conjunction.Left) && PlanGuard(conjunction.Right);
                }

                if (guard is not Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module,
                        DynamicRemote: false,
                        Arguments: [Expr.Variable variable]
                    } call || !owner.ResolvesErlangBif(call)
                    || !bindings.TryGetValue(variable.Name, out var binding))
                {
                    return false;
                }

                byte opcode = TypeGuardOpcode(call);
                if (opcode == 0)
                {
                    return false;
                }

                int register = binding.Input;
                if (binding.Field is int field)
                {
                    register = nextRegister;
                    int destination = register;
                    checks.Add(() => owner.EmitTuplePart(binding.Input, field, destination));
                    if (used is null || used.Contains(variable.Name))
                    {
                        nextRegister++;
                        bindings[variable.Name] = (register, null);
                    }
                }

                checks.Add(() =>
                {
                    owner.Op(opcode, LabelTag, failure);
                    owner.Operand(XRegisterTag, register);
                });

                return true;
            }

            int ReserveNestedProjection(int input)
            {
                if (input > 0 && input < arity && freeSecondaryProjection is int register)
                {
                    freeSecondaryProjection = null;

                    return register;
                }

                return nextRegister++;
            }

            bool PlanConsTupleField(
                int input,
                int field,
                Pattern.List list,
                Pattern.Variable tail
            )
            {
                Pattern head = list.Items[0];
                Pattern.Variable? alias = null;
                Pattern shape = head;
                if (head is AliasPattern aliased && TryReadPatternAlias(aliased, out var name, out var inner))
                {
                    alias = name;
                    shape = inner;
                }

                if (shape is not Pattern.Tuple tuple)
                {
                    return false;
                }

                // Keep both incoming arguments intact for every unchecked shape edge.
                int headRegister = nextRegister++;
                int tailRegister = nextRegister++;
                if (alias is not null && !AddBinding(alias, headRegister, null))
                {
                    return false;
                }

                checks.Add(() =>
                {
                    owner.EmitTuplePart(input, field, headRegister);
                    owner.EmitNonemptyList(headRegister, failure);
                    owner.EmitListParts(
                        headRegister,
                        XRegisterTag,
                        headRegister,
                        XRegisterTag,
                        tailRegister
                    );
                });

                if (!PlanTuple(tuple, headRegister) || !AddBinding(tail, tailRegister, null))
                {
                    return false;
                }

                (checkedConsHeads ??= []).Add(head);

                return true;
            }

            bool PlanTuple(Pattern.Tuple tuple, int input, int? checkedColumn = null)
            {
                bool tagged = HasTupleTag(tuple, out Atom tag);
                int reject = failure;
                if (tagged && sharedEntry is { EntryFailure: var outside, Fallback: { } finalFallback }
                    && input < arity && EntryShape(finalFallback.Patterns[input]) is Pattern.Tuple final
                    && final.Items.Count == tuple.Items.Count && HasTupleTag(final, out Atom finalTag) && tag.Equals(finalTag))
                {
                    reject = outside;
                }

                if (checkedColumn is null && (input != 0 || checkedRootFields is null))
                {
                    checks.Add(tagged
                        ? () => EmitDetailRecordTest(
                            input,
                            tuple.Items.Count,
                            tag,
                            reject
                        )
                        : () => EmitTupleShapeTest(
                            XRegisterTag,
                            input,
                            tuple.Items.Count,
                            failure
                        ));
                }
                for (int index = tagged ? 1 : 0; index < tuple.Items.Count; index++)
                {
                    if (checkedColumn == index || input == 0 && checkedRootFields?.Contains(index) == true)
                    {
                        continue;
                    }

                    int field = index;
                    int scratch = nextRegister;
                    switch (tuple.Items[index])
                    {
                        case Pattern.Any { }:
                            break;
                        case Pattern.Variable variable when checkedColumn is not null
                            && sharedNested is not null && sharedNested.Projections.TryGetValue(field, out int projected):
                            if (!AddBinding(variable, projected, null))
                            {
                                return false;
                            }

                            break;
                        case Pattern.Variable variable when AddBinding(variable, input, field):
                            break;
                        case Pattern.Literal literal:
                            int operand = selector is int consumed
                                && sharedEntry?.ProjectedRootFields?.Values.Contains(input) == true
                                ? consumed
                                : scratch;
                            checks.Add(() =>
                            {
                                owner.EmitTuplePart(input, field, operand);
                                owner.Op(ExactEqualTest, LabelTag, failure);
                                owner.Operand(XRegisterTag, operand);
                                owner.LiteralOperand(literal.Value);
                            });
                            break;
                        case Pattern.List { Items: [_], Tail: Pattern.Variable tail } list
                            when sharedEntry?.AllowConsTupleField == true:
                            if (!PlanConsTupleField(
                                input,
                                field,
                                list,
                                tail
                            ))
                            {
                                return false;
                            }

                            break;
                        case Pattern.Tuple nested when input == 0 && sharedNested?.Field == field:
                            if (!PlanTuple(nested, sharedNested.Register, sharedNested.Selector))
                            {
                                return false;
                            }

                            break;
                        case Pattern.Tuple nested when input == 0
                            && sharedEntry?.ProjectedRootFields?.TryGetValue(field, out int projectedRootRegister) == true:
                            if (!PlanTuple(nested, projectedRootRegister))
                            {
                                return false;
                            }

                            break;
                        case AliasPattern alias when (sharedEntry?.CheckedAtomArgument is not null
                            || sharedEntry?.ProjectedRootFields is not null)
                            && TryReadPatternAlias(alias, out var nestedAlias, out var nestedShape) && nestedShape is Pattern.Tuple aliased:
                            int nestedRegister = ReserveNestedProjection(input);
                            checks.Add(() => owner.EmitTuplePart(input, field, nestedRegister));
                            if (!AddBinding(nestedAlias, nestedRegister, null) || !PlanTuple(aliased, nestedRegister))
                            {
                                return false;
                            }

                            break;
                        case Pattern.Tuple nested:
                            int tupleRegister = ReserveNestedProjection(input);
                            checks.Add(() => owner.EmitTuplePart(input, field, tupleRegister));
                            if (!PlanTuple(nested, tupleRegister))
                            {
                                return false;
                            }

                            break;
                        default:
                            return false;
                    }
                }

                return true;
            }
        }

        private bool HasProvenEntryTupleShape(
            FunctionDefinition function,
            int input,
            int arity,
            Atom? tag,
            bool includeCallResults = false
        )
        {
            return !owner.IsDeclaredNative((function.Name, function.Arity))
                && HasOnlyLocalCalls(
                    function,
                    function.Arity,
                    (caller, invocation) =>
                {
                    Expr argument = invocation.Arguments[input];
                    if (argument is Expr.RecordConstruction { Wildcard: null } record)
                    {
                        argument = RecordExpressions.Create(record);
                    }

                    if (argument is Expr.Tuple tuple)
                    {
                        return tuple.Items.Count == arity && (tag is null || tuple.Items[0] is Expr.Literal { Value: Atom value }
                            && value.Equals(tag));
                    }

                    if (includeCallResults && tag is null && argument is Expr.Call call)
                    {
                        return HasFixedTupleCallResult(call, arity);
                    }

                    return argument is Expr.Variable variable && caller.Clauses.Any(clause =>
                        clause.Patterns.Any(pattern => pattern is AliasPattern
                        {
                            Left: Pattern.Tuple matched, Right: Pattern.Variable alias
                        } && alias.Name == variable.Name && matched.Items.Count == arity
                            && (tag is null || matched.Items[0] is Pattern.Literal { Value: Atom value } && value.Equals(tag)))
                        && HasUnshadowedInvocation(clause, invocation));
                }
                );
        }

        private static bool HasUnshadowedInvocation(Clause clause, Expr.Call invocation)
        {
            var expressions = LocalFunctionEntries.ClauseExpressions([clause]).SelectMany(LocalFunctionEntries.Walk).ToArray();

            return expressions.Any(expression => ReferenceEquals(expression, invocation))
                && !expressions.Any(expression =>
                    expression is Expr.Fun or Expr.ListComprehension or Expr.BinaryComprehension or Expr.MapComprehension
                    && LocalFunctionEntries.Walk(expression).Any(nested => ReferenceEquals(nested, invocation)));
        }

        private bool TryEmitTupleEntryWithRegisterFields(
            Clause clause,
            int failure,
            int? baseSlot,
            Action? reserveFailure = null,
            bool checkShape = false,
            int? groupedInput = null
        )
        {
            int tupleInput = groupedInput ?? 0;
            bool singleEntry = baseSlot is null;
            if (singleEntry && reserveFailure is null)
            {
                tupleInput = Array.FindIndex(clause.Patterns.ToArray(), pattern => pattern is Pattern.Tuple);
            }

            if (clause.Guard is not null || tupleInput < 0)
            {
                return false;
            }

            Pattern root = clause.Patterns[tupleInput];
            Pattern.Variable? tupleAlias = null;
            if (groupedInput is not null && root is AliasPattern alias
                && TryReadPatternAlias(alias, out var aliasVariable, out var shape))
            {
                tupleAlias = aliasVariable;
                root = shape;
            }

            if (root is not Pattern.Tuple tuple)
            {
                return false;
            }

            bool selectorCase = ReadCallSelectorCase(clause.Body, out _) is not null;
            if (singleEntry && !selectorCase && clause.Body is not Expr.Call { DynamicRemote: false })
            {
                return false;
            }

            bool needsShape = singleEntry || checkShape;
            Atom? tag = needsShape && HasTupleTag(tuple, out Atom entryTag) ? entryTag : null;
            var names = new HashSet<string>(StringComparer.Ordinal);
            var bindings = new List<(string Name, int Register)>();
            var fields = new List<(int Index, int Register)>();
            var tests = new List<(int Index, Term Literal)>();
            for (int index = 0; index < clause.Patterns.Count; index++)
            {
                if (index == tupleInput)
                {
                    continue;
                }

                if (clause.Patterns[index] is not Pattern.Variable parameter || !AddBinding(parameter, index))
                {
                    return false;
                }
            }

            if (tupleAlias is not null && !AddBinding(tupleAlias, tupleInput))
            {
                return false;
            }

            int live = clause.Patterns.Count;
            for (int index = tag is null ? 0 : 1; index < tuple.Items.Count; index++)
            {
                switch (tuple.Items[index])
                {
                    case Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                        break;
                    case Pattern.Variable variable when AddBinding(variable, live):
                        fields.Add((index, live++));
                        break;
                    case Pattern.Literal { Value: Atom or Integer or Nil } literal:
                        tests.Add((index, literal.Value));
                        break;
                    default:
                        return false;
                }
            }

            if (live >= BeamWire.MaximumArity)
            {
                return false;
            }

            if (groupedInput is not null && owner.SsaTypeInferenceEnabled && !owner.ModuleOptimizationDisabled
                && clause.Body is Expr.Sequence { Items: [Expr.Call, Expr.Tuple] }
                && HasTupleTag(tuple, out _))
            {
                int argumentCount = clause.Patterns.Count;
                var effectInputs = bindings.ToDictionary(
                    binding => binding.Name,
                    binding => binding.Register < argumentCount
                        ? (binding.Register, (int?)null)
                        : (tupleInput, (int?)fields[binding.Register - argumentCount].Index),
                    StringComparer.Ordinal
                );
                if (TryEmitSingleEntryEffect(
                    clause,
                    effectInputs,
                    EmitTests,
                    projectCheckedFieldsInOrder: true
                ))
                {
                    return true;
                }
            }

            if (selectorCase && tests.Count == 0 && fields is [var projection])
            {
                var registerNames = Enumerable.Repeat(VariableScopeNames.Wildcard, live).ToArray();
                foreach (var (binding, register) in bindings)
                {
                    registerNames[register] = binding;
                }

                var prepared = clause with
                {
                    Patterns = registerNames.Select(name => (Pattern)new Pattern.Variable(name)).ToArray()
                };
                if (ReadEntryRegisterCase(
                    prepared,
                    (tupleInput, projection.Index, projection.Register),
                    projectAnyArgument: true
                ) is { Deferred: not null } registerCase)
                {
                    bool proven = singleEntry && reserveFailure is null
                        && parentFunction is { Clauses.Count: 1 } function
                        && HasProvenEntryTupleShape(
                            function,
                            tupleInput,
                            tuple.Items.Count,
                            tag
                        );
                    if (!proven)
                    {
                        EmitTests();
                    }

                    EmitEntryRegisterCase(registerCase);

                    return true;
                }
            }

            Expr.Call? target = null;
            var moves = new List<(int Destination, int Source)>();
            var constants = new List<(int Destination, Term Value)>();
            var usedRegisters = new HashSet<int>();
            if (clause.Body is Expr.Call { DynamicRemote: false } call)
            {
                Expr.Call resolved = owner.ResolveImportedCall(call, entries);
                if (resolved.Module != ErlangModuleNames.Module
                    && (resolved.Module is not null || entries.ContainsKey((resolved.Function, resolved.Arguments.Count)))
                    && (!singleEntry || resolved.Module is null && tests.Count == 0 && fields.Count >= 1))
                {
                    target = resolved;
                    var registers = bindings.ToDictionary(binding => binding.Name, binding => binding.Register,
                        StringComparer.Ordinal);
                    for (int index = 0; index < resolved.Arguments.Count; index++)
                    {
                        Expr argument = resolved.Arguments[index];
                        if (argument is Expr.Variable variable && registers.TryGetValue(variable.Name, out int register))
                        {
                            if (register < clause.Patterns.Count
                                && clause.Patterns[register] is Pattern.Variable parameter && parameter.Name == variable.Name
                                && parentFunction is { } function
                                && ReadPrivateIntegerConstant(function, register, new()) is { } integer)
                            {
                                constants.Add((index, integer));
                                continue;
                            }

                            usedRegisters.Add(register);
                            if (register != index || singleEntry || groupedInput is not null)
                            {
                                moves.Add((index, register));
                            }
                        }
                        else if (AttributeLiteral.TryNormalizeConstant(argument, out Term constant))
                        {
                            constants.Add((index, constant));
                        }
                        else
                        {
                            target = null;
                            break;
                        }
                    }
                }
            }

            if (target is null && tupleInput == 0 && tupleAlias is null && clause.Body is Expr.Call constructedCall)
            {
                int lastRegister = fields.Count == 0 ? -1 : fields[^1].Register;
                var registers = bindings.ToDictionary(binding => binding.Name,
                    binding => binding.Register == lastRegister ? 0 : binding.Register, StringComparer.Ordinal);
                if (TryEmitRegisterConstructedArgument(
                    constructedCall,
                    registers,
                    (clause.Patterns.Count, EmitFields),
                    EmitTests
                ))
                {
                    return true;
                }

                void EmitFields()
                {
                    foreach (var (index, register) in fields)
                    {
                        owner.EmitTuplePart(tupleInput, index, register == lastRegister ? tupleInput : register);
                    }
                }
            }

            if (target is not null)
            {
                bool proven = owner.SsaTypeInferenceEnabled && !owner.ModuleOptimizationDisabled
                    && owner.OnLoadFunction is null && singleEntry && reserveFailure is null && tag is null
                    && parentFunction is { Clauses.Count: 1 } function
                    && HasProvenEntryTupleShape(
                        function,
                        tupleInput,
                        tuple.Items.Count,
                        tag,
                        includeCallResults: true
                    );
                EmitTestsWithShape(proven);
                int lastRegister = singleEntry || groupedInput is not null && !usedRegisters.Contains(tupleInput)
                    ? fields.Where(field => usedRegisters.Contains(field.Register)).Select(field => field.Register).LastOrDefault(-1)
                    : -1;
                if (moves.Any(move => move.Source == lastRegister && move.Destination == lastRegister))
                {
                    lastRegister = -1;
                }
                foreach (var (index, register) in fields)
                {
                    if (usedRegisters.Contains(register))
                    {
                        owner.EmitTuplePart(tupleInput, index, register == lastRegister ? tupleInput : register);
                    }
                }

                if (lastRegister >= 0)
                {
                    for (int index = 0; index < moves.Count; index++)
                    {
                        var (destination, source) = moves[index];
                        if (source == lastRegister)
                        {
                            moves[index] = (destination, tupleInput);
                        }
                    }
                }

                moves.RemoveAll(move => move.Destination == move.Source);
                if (!singleEntry || !TryEmitTupleTailRotation(tupleInput, moves, constants))
                {
                    owner.EmitArgumentMoves(moves, constants);
                }
                EmitRegisterTailTarget(target, target.Arguments.Count);

                return true;
            }

            if (groupedInput is not null && clause.Body is Expr.Variable variableResult
                && bindings.FindIndex(binding => binding.Name == variableResult.Name) is >= 0 and var resultIndex)
            {
                EmitTests();
                int source = bindings[resultIndex].Register;
                if (source > 0 && source < clause.Patterns.Count)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        source,
                        XRegisterTag,
                        0
                    );
                }
                else if (source >= clause.Patterns.Count)
                {
                    owner.EmitTuplePart(tupleInput, fields[source - clause.Patterns.Count].Index, 0);
                }

                owner.instructions.WriteByte(Return);

                return true;
            }

            if (groupedInput is not null && AttributeLiteral.TryNormalizeConstant(clause.Body, out Term constantResult))
            {
                EmitTests();
                owner.EmitConstant(constantResult, 0);
                owner.instructions.WriteByte(Return);

                return true;
            }

            if (baseSlot is not int fallbackSlot)
            {
                return false;
            }

            EmitTests();
            nextSlot = fallbackSlot;
            variables = new(StringComparer.Ordinal);
            var parameters = bindings.Select(binding => new Pattern.Variable(binding.Name)).ToArray();
            if (ReadUsedEntryParameters(new Clause(parameters, null, clause.Body)) is { } used)
            {
                bindings.RemoveAll(binding => !used.Contains(binding.Name));
            }

            int arity = clause.Patterns.Count;
            EmitFrameAllocation(arity, frameSize);
            EmitFrameInitialization(bindings.Count, frameSize);
            foreach (var (name, register) in bindings)
            {
                int slot = fallbackSlot + bindings.Count - 1 - (Slot() - fallbackSlot);
                if (register < arity)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        register,
                        YRegisterTag,
                        slot
                    );
                }
                else
                {
                    owner.Op(GetTupleElement, XRegisterTag, tupleInput);
                    owner.Operand(BeamWire.UnsignedTag, fields[register - arity].Index);
                    owner.Operand(YRegisterTag, slot);
                }

                variables.Add(name, slot);
            }

            if (!TailLocal(clause.Body))
            {
                Expression(clause.Body);
                owner.EmitFrameReturn(frameSize);
            }

            return true;

            void EmitTests() => EmitTestsWithShape(skipShape: false);

            void EmitTestsWithShape(bool skipShape)
            {
                reserveFailure?.Invoke();
                if (needsShape && !skipShape)
                {
                    if (tag is not null)
                    {
                        EmitDetailRecordTest(
                            tupleInput,
                            tuple.Items.Count,
                            tag,
                            failure
                        );
                    }
                    else
                    {
                        EmitTupleShapeTest(
                            XRegisterTag,
                            tupleInput,
                            tuple.Items.Count,
                            failure
                        );
                    }
                }

                int scratch = clause.Patterns.Count;
                foreach (var (index, literal) in tests)
                {
                    owner.EmitTuplePart(tupleInput, index, scratch);
                    owner.Op(ExactEqualTest, LabelTag, failure);
                    owner.Operand(XRegisterTag, scratch);
                    owner.LiteralOperand(literal);
                }
            }

            bool AddBinding(Pattern.Variable variable, int register)
            {
                if (variable.Name == VariableScopeNames.Wildcard)
                {
                    return true;
                }

                if (!names.Add(variable.Name))
                {
                    return false;
                }

                bindings.Add((variable.Name, register));

                return true;
            }
        }

        private bool TryEmitTupleTailRotation(
            int start, List<(int Destination, int Source)> moves, List<(int Destination, Term Value)> constants)
        {
            if (moves.Count < 4 || constants.Count != 0)
            {
                return false;
            }

            for (int index = 0; index < moves.Count; index++)
            {
                if (moves[index] != (start + index, start + (index + 1) % moves.Count))
                {
                    return false;
                }
            }

            for (int register = start + 2; register < start + moves.Count; register++)
            {
                owner.Op(Swap, XRegisterTag, register);
                owner.Operand(XRegisterTag, register - 1);
            }

            owner.Op(Swap, XRegisterTag, start);
            owner.Operand(XRegisterTag, start + moves.Count - 1);

            return true;
        }
    }
}
