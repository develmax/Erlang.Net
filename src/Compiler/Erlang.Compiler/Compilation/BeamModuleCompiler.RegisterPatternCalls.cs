using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitEntryAtomTailDispatch(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Case { Value: Expr.Variable selected, Clauses.Count: >= 3 } decision
                || decision.Clauses[^1] is not { Guard: null, Patterns: [Pattern.Variable otherwise], Body: Expr.Call exit })
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            int selector = Array.IndexOf(parameters, selected.Name);
            if (selector < 0 || !TryResolveOrdinaryCallTarget(exit, out string? exitModule, allowControlTransfer: true)
                || exitModule != ErlangModuleNames.Module)
            {
                return false;
            }

            int bound = otherwise.Name == VariableScopeNames.Wildcard ? -1 : Array.IndexOf(parameters, otherwise.Name);
            int reasonSource = selector;
            var error = ReadTaggedExit(exit, selected.Name);
            if (error is null && otherwise.Name != VariableScopeNames.Wildcard)
            {
                error = ReadTaggedExit(exit, otherwise.Name);
                reasonSource = bound >= 0 ? bound : selector;
            }

            if (error is not { } tagged)
            {
                return false;
            }

            var branches = new List<(Atom Key, EntryCall Tail)>();
            var keys = new HashSet<Atom>();
            foreach (Clause branch in decision.Clauses.SkipLast(1))
            {
                if (branch is not { Guard: null, Patterns: [Pattern.Literal { Value: Atom key }], Body: Expr.Call call }
                    || !keys.Add(key))
                {
                    return false;
                }

                Expr.Call specialized = call with
                {
                    Arguments = call.Arguments.Select(argument => argument is Expr.Variable variable
                        && variable.Name == selected.Name ? new Expr.Literal(key) : argument).ToArray()
                };
                if (ReadEntryCall(specialized, parameters) is not { Module: null } tail)
                {
                    return false;
                }

                branches.Add((key, tail));
            }

            branches.Sort((left, right) => TermOrder.Compare(right.Key, left.Key, exact: true));
            int[] labels = branches.Select(_ => Label()).ToArray();
            int fallback = Label();
            owner.EmitMembershipSelect(
                selector,
                fallback,
                branches.Select((branch, index) => ((Term)branch.Key, labels[index])).Reverse()
            );
            for (int index = 0; index < branches.Count; index++)
            {
                Mark(labels[index]);
                EntryCall tail = branches[index].Tail;
                int arity = tail.Call.Arguments.Count;
                owner.EmitOrderedTailMoves(tail.Moves, tail.Constants);
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, arity);
                owner.Operand(LabelTag, entries[(tail.Call.Function, arity)]);
            }

            Mark(fallback);
            bool compareBound = bound >= 0 && bound != selector;
            int mismatch = 0;
            if (compareBound)
            {
                mismatch = Label();
                owner.Op(ExactEqualTest, LabelTag, mismatch);
                owner.Operand(XRegisterTag, selector);
                owner.Operand(XRegisterTag, bound);
            }

            EmitTaggedExit(
                tagged.Call,
                tagged.Tags,
                location,
                framed: false,
                live: reasonSource + 1,
                source: reasonSource
            );
            if (compareBound)
            {
                Mark(mismatch);
                var caseLocation = new BeamSourceLocation(decision.PreprocessorFile, decision.SourceLine);
                owner.EmitLine(caseLocation.Line == 0 ? location : caseLocation);
                owner.Op(CaseEnd, XRegisterTag, selector);
            }

            return true;
        }

        private bool TryEmitEntryAtomCallTails(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            Expr body = clause.Body;
            if (body is Expr.Sequence
                {
                    Items: [Expr.Match { Pattern: Pattern.Variable binding, Value: Expr.Literal { Value: Atom value } },
                        Expr.Case following]
                }
                && binding.Name != VariableScopeNames.Wildcard && !parameters.Contains(binding.Name))
            {
                body = SubstituteConstants(following,
                    new Dictionary<string, Term>(StringComparer.Ordinal) { [binding.Name] = value });
            }

            if (body is not Expr.Case { Value: Expr.Call read, Clauses.Count: >= 2 } decision
                || !decision.Clauses.Any(branch => branch.Body is Expr.Call { DynamicRemote: true })
                || ReadUsedEntryParameters(clause with
                {
                    Body = decision with { Value = new Expr.Literal(Nil.Value) }
                }) is not { } used)
            {
                return false;
            }

            int[] survivors = Enumerable.Range(0, parameters.Length)
                .Where(index => used.Contains(parameters[index])).Reverse().ToArray();
            if (ReadEntrySelector(
                read,
                parameters,
                survivors,
                trimFrame: false
            ) is not { } selector)
            {
                return false;
            }

            var branches = new List<(Atom Key, EntryCall Plan, int? DynamicArity)>();
            var keys = new HashSet<Atom>();
            foreach (Clause branch in decision.Clauses)
            {
                if (branch is not { Guard: null, Patterns: [Pattern.Literal { Value: Atom key }], Body: Expr.Call call }
                    || !keys.Add(key))
                {
                    return false;
                }

                if (!call.DynamicRemote && ReadEntryCall(call, [], selector.StackInputs) is { } ordinary)
                {
                    branches.Add((key, ordinary, null));
                }
                else if (call.DynamicRemote && ReadDynamicTail(call, selector.StackInputs) is { } dynamic)
                {
                    branches.Add((key, dynamic.Plan, dynamic.Arity));
                }
                else
                {
                    return false;
                }
            }

            branches.Sort((left, right) => TermOrder.Compare(left.Key, right.Key, exact: true));
            int frame = selector.Saved.Length - EmitEntrySelector(selector, read, parameters);
            var previous = CallLocation(read, location);
            var labels = new int[branches.Count];
            for (int index = labels.Length - 1; index >= 0; index--)
            {
                labels[index] = Label();
            }

            int failure = Label();
            owner.EmitMembershipSelect(0, failure, branches.Select((branch, index) => ((Term)branch.Key, labels[index])));
            for (int index = branches.Count - 1; index >= 0; index--)
            {
                Mark(labels[index]);
                var (_, tail, dynamicArity) = branches[index];
                owner.EmitOrderedTailMoves(tail.Moves, tail.Constants, tail.SavedInputs);
                if (dynamicArity is int arity)
                {
                    owner.EmitCallLine(tail.Call, previous);
                    owner.Op(TailDynamicApply, BeamWire.UnsignedTag, arity);
                    owner.Operand(BeamWire.UnsignedTag, frame);
                }
                else
                {
                    EmitPreparedFrameTail(tail, previous, frame);
                }
            }

            Mark(failure);
            owner.EmitLine(previous);
            owner.Op(CaseEnd, XRegisterTag, 0);

            return true;
        }

        private static (EntryCall Plan, int Arity)? ReadDynamicTail(Expr.Call call, string[] savedInputs, string? result = null)
        {
            if (call.Arguments is not [var module, var function, Expr.List { Tail: null } arguments]
                || arguments.Items.Count > BeamWire.MaximumArity - 2)
            {
                return null;
            }

            Expr[] operands = [.. arguments.Items, module, function];
            var registers = new List<(int Destination, int Source)>();
            var moves = new List<(int Destination, int Source)>();
            var constants = new List<(int Destination, Term Value)>();
            for (int index = 0; index < operands.Length; index++)
            {
                if (operands[index] is Expr.Variable current && current.Name == result)
                {
                    if (index != 0)
                    {
                        registers.Add((index, 0));
                    }
                }
                else if (operands[index] is Expr.Variable { Name: not VariableScopeNames.Wildcard } variable
                    && Array.IndexOf(savedInputs, variable.Name) is int slot && slot >= 0)
                {
                    moves.Add((index, slot));
                }
                else if (AttributeLiteral.TryNormalizeConstant(operands[index], out Term constant))
                {
                    constants.Add((index, constant));
                }
                else
                {
                    return null;
                }
            }

            return (new EntryCall(
                call,
                null,
                registers,
                constants,
                moves
            ), arguments.Items.Count);
        }

        private bool TryEmitEffectfulTupleOrAtomCase(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                {
                    Guard: null, Patterns: [Pattern.Literal { Value: Atom }], Body: Expr.Sequence
                    {
                        Items: [Expr.Case
                        {
                            Value: Expr.Call read, Clauses:
                            [
                            { Guard: null, Patterns: [Pattern.Tuple tuple], Body: var matched },
                            { Guard: null, Patterns: [Pattern.Literal { Value: Atom atom }], Body: Expr.Call effect }
                            ]
                        } decision, _, ..]
                    } sequence
                } clause
                ]
                || !HasTupleTag(tuple, out Atom tag)
                || tuple.Items.Skip(1).Any(item => item is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
                || !AttributeLiteral.TryNormalizeConstant(matched, out _)
                || !AttributeLiteral.TryNormalizeConstant(sequence.Items[^1], out Term result)
                || ReadEntryCall(read, []) is not { } selector || ReadEntryCall(effect, []) is not { } alternative)
            {
                return false;
            }

            var continuation = new List<EntryCall>();
            foreach (Expr item in sequence.Items.Skip(1).SkipLast(1))
            {
                if (item is not Expr.Call call || ReadEntryCall(call, []) is not { } next)
                {
                    return false;
                }

                continuation.Add(next);
            }

            owner.EmitLiteralTest(clause, owner.nextLabel - 2);
            EmitEntryCallWithEmptyFrame(selector, []);
            int atomBranch = Label();
            int joined = Label();
            int failure = Label();
            owner.Op(IsTuple, LabelTag, atomBranch);
            owner.Operand(XRegisterTag, 0);
            owner.Op(IsTaggedTuple, LabelTag, failure);
            owner.EmitTypedRegister(0, TupleTypeBits);
            owner.Operand(BeamWire.UnsignedTag, tuple.Items.Count);
            owner.LiteralOperand(tag);
            JumpTo(joined);

            Mark(atomBranch);
            owner.Op(ExactEqualTest, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(atom);
            var previous = CallLocation(read, location);
            EmitEntryCall(alternative, previous);

            Mark(joined);
            foreach (EntryCall next in continuation)
            {
                EmitEntryCall(next, previous);
                previous = CallLocation(next.Call, previous);
            }

            owner.EmitConstant(result, 0);
            owner.EmitFrameReturn(0);
            Mark(failure);
            var failedAt = new BeamSourceLocation(decision.PreprocessorFile, decision.SourceLine);
            owner.EmitLine(failedAt.Line == 0 ? CallLocation(read, location) : failedAt);
            owner.Op(CaseEnd, XRegisterTag, 0);

            return true;
        }

        private bool TryEmitEntryTupleOrAtomCase(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Case
                {
                    Value: Expr.Call read, Clauses:
                    [
                    { Guard: null, Patterns: [Pattern.Tuple tuple], Body: Expr.Variable result },
                    { Guard: null, Patterns: [Pattern.Literal { Value: Atom atom }], Body: var otherwise }
                    ]
                } decision
                || ReadSingleTupleField(tuple) is not { Field: Pattern.Variable field } access
                || field.Name == VariableScopeNames.Wildcard || field.Name != result.Name
                || !AttributeLiteral.TryNormalizeConstant(otherwise, out Term constant))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Contains(field.Name, StringComparer.Ordinal)
                || ReadEntryCall(read, parameters) is not { } selector)
            {
                return false;
            }

            EmitFrameAllocation(EntryCallLiveCount(selector, parameters), 0);
            EmitEntryCall(selector);
            int atomBranch = Label();
            int failure = Label();
            owner.Op(IsTuple, LabelTag, atomBranch);
            int selectorType = RegisterCaseSelectorType(selector, IsTuple);
            if (selectorType == 0)
            {
                owner.Operand(XRegisterTag, 0);
            }
            else
            {
                owner.EmitTypedRegister(0, selectorType);
            }

            owner.Op(IsTaggedTuple, LabelTag, failure);
            owner.EmitTypedRegister(0, TupleTypeBits);
            owner.Operand(BeamWire.UnsignedTag, tuple.Items.Count);
            owner.LiteralOperand(access.Tag);
            owner.EmitTuplePart(0, access.Index, 0);
            owner.Op(Deallocate, BeamWire.UnsignedTag, 0);
            owner.instructions.WriteByte(Return);

            Mark(atomBranch);
            if (!HasKnownNonTupleResult(selector, atom))
            {
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.Operand(XRegisterTag, 0);
                owner.LiteralOperand(atom);
            }

            owner.EmitConstant(constant, 0);
            owner.Op(Deallocate, BeamWire.UnsignedTag, 0);
            owner.instructions.WriteByte(Return);

            Mark(failure);
            var previous = CallLocation(read, location);
            var failedAt = new BeamSourceLocation(decision.PreprocessorFile, decision.SourceLine);
            if (failedAt.Line != 0 && failedAt != previous)
            {
                owner.EmitLine(failedAt);
            }

            owner.Op(CaseEnd, XRegisterTag, 0);

            return true;
        }

        private sealed record EntryTupleBinding(
            Pattern.Tuple Pattern,
            HashSet<string> Survivors,
            bool KnownShape,
            int ForwardedField,
            ForwardedCall? NextCall
        );

        private EntryTupleBinding? ReadEntryTupleBinding(
            Expr.Match match,
            string[] parameters,
            Expr.Sequence continuation,
            EntryCall call
        )
        {
            if (match.Pattern is not Pattern.Tuple tuple
                || tuple.Items.Any(item => item is not (Erlang.Pattern.Variable or Erlang.Pattern.Any or Erlang.Pattern.Literal)))
            {
                return null;
            }

            var incoming = parameters.ToHashSet(StringComparer.Ordinal);
            var fields = tuple.Items.OfType<Pattern.Variable>()
                .Where(field => field.Name != VariableScopeNames.Wildcard).ToArray();
            var newFields = fields.Where(field => !incoming.Contains(field.Name)).ToArray();
            if (newFields.Select(field => field.Name).Distinct(StringComparer.Ordinal).Count() != newFields.Length)
            {
                return null;
            }

            var bindings = parameters.Select(name => (Pattern)new Pattern.Variable(name)).Concat(newFields).ToArray();
            if (ReadUsedEntryParameters(new Clause(bindings, null, continuation)) is not { } used)
            {
                return null;
            }

            var survivors = used.Where(incoming.Contains).ToHashSet(StringComparer.Ordinal);
            survivors.UnionWith(fields.Select(field => field.Name).Where(incoming.Contains));
            bool knownShape = tuple.Items.All(item => item is Pattern.Any
                || item is Pattern.Variable variable && !incoming.Contains(variable.Name))
                && call.Module is null && !owner.ModuleOptimizationDisabled
                && !owner.IsDeclaredNative((call.Call.Function, call.Call.Arguments.Count))
                && ReturnsTuple(
                    call.Call,
                    tuple.Items.Count,
                    [],
                    exact: true
                );
            var candidates = bindings.Cast<Pattern.Variable>().Select(binding => binding.Name).ToArray();
            ForwardedCall? next = null;
            int forwardedField = -1;
            var items = tuple.Items.ToArray();
            for (int index = 0; index < items.Length; index++)
            {
                if (items[index] is not Pattern.Variable { Name: not VariableScopeNames.Wildcard } field
                    || incoming.Contains(field.Name))
                {
                    continue;
                }

                if (!used.Contains(field.Name))
                {
                    items[index] = new Pattern.Variable(VariableScopeNames.Wildcard);
                }
                else if (next is null && ReadForwardedCall(
                    continuation, field.Name, candidates.Where(name => name != field.Name).ToArray()
                ) is { ReusedInput: null } forwarded)
                {
                    next = forwarded;
                    forwardedField = index;
                    items[index] = new Pattern.Variable(VariableScopeNames.Wildcard);
                }
            }

            return new(
                tuple with { Items = items },
                survivors,
                knownShape,
                forwardedField,
                next
            );
        }

        private void EmitEntryTupleBinding(
            Expr.Match match,
            Pattern.Tuple tuple,
            bool knownShape,
            int forwardedField,
            BeamSourceLocation previous
        )
        {
            int failure = knownShape ? 0 : Label();
            Pattern(
                tuple,
                0,
                failure,
                checkedShape: knownShape ? tuple : null,
                entryPatternScratch: true,
                valueTag: XRegisterTag
            );
            if (!knownShape)
            {
                int done = Label();
                JumpTo(done);
                EmitTaggedCallMatchFailure(
                    match,
                    failure,
                    previous,
                    previous
                );
                Mark(done);
            }

            if (forwardedField >= 0)
            {
                owner.EmitTuplePart(0, forwardedField, 0);
            }
        }

        private bool TryEmitEntryTupleMatchResult(Clause clause)
        {
            return clause.Guard is null && HasUnconditionalParameters(clause)
                && TryEmitEntryTupleMatchResult(
                    clause.Body,
                    clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray(),
                    out _
                );
        }

        private bool TryEmitEntryTupleMatchResult(
            Expr expression,
            string[] parameters,
            out BeamSourceLocation? lastEmittedLine,
            List<(int Label, BeamSourceLocation Location)>? deferredFailures = null,
            string[]? framedInputs = null
        )
        {
            lastEmittedLine = null;
            if (expression is not Expr.Sequence
                {
                    Items: [
                    Expr.Match { Pattern: Pattern.Tuple tuple, Value: Expr.Call read } match,
                    Expr.Variable or Expr.Call or Expr.Tuple
                ]
                } sequence)
            {
                return false;
            }

            var checks = new List<(int Field, int Source, Term? Literal)>();
            var bound = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int field = 0; field < tuple.Items.Count; field++)
            {
                switch (tuple.Items[field])
                {
                    case Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                        break;
                    case Pattern.Variable variable:
                        int source = Array.IndexOf(parameters, variable.Name);
                        if (source >= 0)
                        {
                            checks.Add((field, source, null));
                        }
                        else if (!bound.TryAdd(variable.Name, field))
                        {
                            return false;
                        }

                        break;
                    case Pattern.Literal literal:
                        checks.Add((field, -1, literal.Value));
                        break;
                    default:
                        return false;
                }
            }

            Expr body = sequence.Items[1];
            var tail = body as Expr.Call;
            IReadOnlyList<Expr> output = body is Expr.Tuple result ? result.Items : tail?.Arguments ?? [];
            var outputNames = output.Select(operand => (operand as Expr.Variable)?.Name).ToArray();
            var reads = output.SelectMany(LocalFunctionEntries.Walk).OfType<Expr.Variable>()
                .Select(variable => variable.Name).ToHashSet(StringComparer.Ordinal);
            int[] survivors = checks.Where(check => check.Source >= 0).Select(check => check.Source)
                .Concat(Enumerable.Range(0, parameters.Length).Where(index => reads.Contains(parameters[index])))
                .Distinct().OrderDescending().ToArray();
            bool isDate = read is { Function: "date", Arguments.Count: 0, DynamicRemote: false }
                && owner.ResolvesErlangBif(read);
            EntrySelector? selector;
            if (framedInputs is not null)
            {
                // The success path is terminal. Entry slots are dead once the producer arguments have been loaded.
                if (bound.Keys.Any(variables.ContainsKey)
                    || output.Any(operand => operand is Expr.Variable variable && !bound.ContainsKey(variable.Name))
                    || ReadEntryCall(read, [], framedInputs) is not { } framedCall)
                {
                    return false;
                }

                selector = new EntrySelector(framedCall, null, [], [], 0, []);
            }
            else
            {
                selector = isDate
                    ? new EntrySelector(
                        new EntryCall(
                            read with { SourceLine = 0 },
                            ErlangModuleNames.Module,
                            [],
                            []
                        ),
                        null,
                        survivors,
                        survivors.Select(source => parameters[source]).ToArray(),
                        0,
                        []
                    )
                    : ReadEntrySelector(read, parameters, survivors);
            }
            if (selector is null)
            {
                return false;
            }

            var (call, _, saved, stackInputs, _, _) = selector;

            var fields = new List<(int Field, int Register)>();
            EntryCall[]? continuation = null;
            (int Tag, int Register, Term? Literal)[]? tupleOperands = null;
            if (body is Expr.Variable returned && bound.TryGetValue(returned.Name, out int resultField))
            {
                fields.Add((resultField, 0));
            }
            else if (body is Expr.Tuple)
            {
                var usedFields = bound.Where(pair => outputNames.Contains(pair.Key)).OrderBy(pair => pair.Value).ToArray();
                if (usedFields.Length == 0 || usedFields.Length >= FirstReservedXRegister)
                {
                    return false;
                }

                var registers = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int index = 0; index < usedFields.Length; index++)
                {
                    var (name, field) = usedFields[index];
                    int register = (index + 1) % usedFields.Length;
                    registers.Add(name, register);
                    fields.Add((field, register));
                }

                tupleOperands = new (int Tag, int Register, Term? Literal)[output.Count];
                for (int index = 0; index < output.Count; index++)
                {
                    if (output[index] is Expr.Variable variable && variable.Name != VariableScopeNames.Wildcard)
                    {
                        if (registers.TryGetValue(variable.Name, out int register))
                        {
                            tupleOperands[index] = (XRegisterTag, register, null);
                            continue;
                        }

                        int slot = Array.IndexOf(saved, Array.IndexOf(parameters, variable.Name));
                        if (slot >= 0)
                        {
                            tupleOperands[index] = (YRegisterTag, slot, null);
                            continue;
                        }
                    }
                    else if (AttributeLiteral.TryNormalizeConstant(output[index], out Term literal))
                    {
                        tupleOperands[index] = (0, 0, literal);
                        continue;
                    }

                    return false;
                }
            }
            else if (tail is not null)
            {
                continuation = ReadTupleMatchContinuation(
                    tail,
                    parameters,
                    stackInputs,
                    bound,
                    fields
                );
                if (continuation is null)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            int trimmed = 0;
            if (framedInputs is null)
            {
                trimmed = EmitEntrySelector(selector, read, parameters);
            }
            else
            {
                owner.EmitOrderedTailMoves(call.Moves, call.Constants, call.SavedInputs);
                if (frameSize > 0)
                {
                    owner.Op(TrimFrame, BeamWire.UnsignedTag, frameSize);
                    owner.Operand(BeamWire.UnsignedTag, 0);
                }

                owner.EmitCallLine(call.Call, location);
                Call(call.Module, call.Call.Function, call.Call.Arguments.Count);
            }
            bool shapeKnown = checks.Count == 0 && call.Module is null && ReturnsTuple(
                call.Call,
                tuple.Items.Count,
                [],
                exact: true
            );
            int failure = shapeKnown ? 0 : Label();
            int selectorType = RegisterCaseSelectorType(call, IsTuple);
            bool tagged = selectorType == 0 && checks is [(0, -1, Atom)];
            if (tagged)
            {
                EmitDetailRecordTest(
                    0,
                    tuple.Items.Count,
                    (Atom)checks[0].Literal!,
                    failure
                );
            }
            else if (!shapeKnown)
            {
                EmitTupleShapeTest(
                    XRegisterTag,
                    0,
                    tuple.Items.Count,
                    failure,
                    selectorType
                );
            }

            foreach (var (field, source, literal) in checks)
            {
                if (tagged && field == 0)
                {
                    continue;
                }

                owner.EmitTuplePart(0, field, 1);
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.Operand(XRegisterTag, 1);
                if (literal is null)
                {
                    owner.Operand(YRegisterTag, Array.IndexOf(saved, source) - trimmed);
                }
                else
                {
                    owner.LiteralOperand(literal);
                }
            }

            if (tupleOperands is not null)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, tupleOperands.Length + 1);
                owner.Operand(BeamWire.UnsignedTag, 1);
            }

            var construction = continuation?[0].ConstructedArgument;
            if (construction is not null)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, construction.HeapWords);
                owner.Operand(BeamWire.UnsignedTag, 1);
                foreach (var (field, register) in fields.Where(field => construction.Operands.Any(operand => operand.Source == field.Register)))
                {
                    owner.EmitTuplePart(0, field, register);
                }

                EmitEntryConstruction(construction);
            }

            foreach (var (field, register) in fields.Where(field => construction is null
                || construction.Operands.All(operand => operand.Source != field.Register)))
            {
                owner.EmitTuplePart(0, field, register);
            }

            if (tupleOperands is not null)
            {
                owner.EmitTupleHeader(XRegisterTag, tupleOperands.Length);
                foreach (var (tag, register, literal) in tupleOperands)
                {
                    if (literal is null)
                    {
                        owner.Operand(tag, tag == YRegisterTag ? register - trimmed : register);
                    }
                    else
                    {
                        owner.LiteralOperand(literal);
                    }
                }
            }

            var callLocation = CallLocation(call.Call, location);
            var lastLine = callLocation;
            if (continuation is null)
            {
                owner.EmitFrameReturn(saved.Length - trimmed);
            }
            else
            {
                for (int index = 0; index < continuation.Length; index++)
                {
                    EntryCall step = continuation[index];
                    var tailMoves = step.SavedInputs?.Select(move => (move.Destination, move.Source - trimmed)).ToList();
                    owner.EmitOrderedTailMoves(step.Moves, step.Constants, tailMoves);
                    if (index == continuation.Length - 1)
                    {
                        EmitPreparedFrameTail(step, lastLine, saved.Length - trimmed);
                        if (step.Module is not null)
                        {
                            lastLine = CallLocation(step.Call, lastLine);
                        }
                    }
                    else
                    {
                        owner.EmitCallLine(step.Call, lastLine);
                        Call(step.Module, step.Call.Function, step.Call.Arguments.Count);
                        lastLine = CallLocation(step.Call, lastLine);
                    }
                }
            }

            lastEmittedLine = lastLine;
            if (shapeKnown)
            {
                return true;
            }

            if (tagged)
            {
                EmitTaggedCallMatchFailure(
                    match,
                    failure,
                    callLocation,
                    lastLine
                );

                return true;
            }

            var matchLocation = match.SourceLine == 0
                ? CallLocation(read, callLocation)
                : new BeamSourceLocation(match.PreprocessorFile, match.SourceLine);
            if (deferredFailures is not null)
            {
                deferredFailures.Add((failure, matchLocation));

                return true;
            }

            Mark(failure);
            if (matchLocation != lastLine)
            {
                owner.EmitLine(matchLocation);
            }

            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }

        private static bool IsSimpleTupleMatchTail(Expr body) =>
            body is Expr.Sequence
            {
                Items: [
                    Expr.Match { Pattern: Pattern.Tuple tuple, Value: Expr.Call producer },
                    Expr.Call continuation
                ]
            }
            && tuple.Items.All(field => field is Pattern.Variable)
            && producer.Arguments.All(IsSimpleOperand)
            && continuation.Arguments.All(IsSimpleOperand);

        private static bool IsSimpleOperand(Expr operand) => operand is Expr.Variable
            || AttributeLiteral.TryNormalizeConstant(operand, out _);

        private bool TryEmitFramedTupleMatchTail(
            Expr body,
            List<(int Label, BeamSourceLocation Location)> failures
        )
        {
            if (!IsSimpleTupleMatchTail(body))
            {
                return false;
            }

            var inputs = new string[nextSlot];
            foreach (var (name, slot) in variables)
            {
                // A stack slot may be bound to several aliases.
                if ((uint)slot >= (uint)inputs.Length || inputs[slot] is not null)
                {
                    return false;
                }

                inputs[slot] = name;
            }

            return TryEmitEntryTupleMatchResult(body, [], out _, failures, framedInputs: inputs);
        }

        private void EmitRegisterTupleMatchFailures(List<(int Label, BeamSourceLocation Location)> failures)
        {
            foreach (var (label, matchLocation) in failures)
            {
                Mark(label);
                owner.EmitLine(matchLocation);
                owner.Op(BadMatch, XRegisterTag, 0);
            }
        }

        private EntryCall[]? ReadTupleMatchContinuation(
            Expr.Call tail,
            string[] parameters,
            string[] stackInputs,
            Dictionary<string, int> bound,
            List<(int Field, int Register)> fields
        )
        {
            if (ReadStraightCalls(FlattenCallArguments([tail]), parameters) is not { Length: > 0 } calls)
            {
                return null;
            }

            var first = calls[0].Call;
            var argumentNames = first.Arguments.Select(operand => (operand as Expr.Variable)?.Name).ToArray();
            var constructedReads = first.Arguments.Where(operand => operand is not Expr.Variable)
                .SelectMany(LocalFunctionEntries.Walk).OfType<Expr.Variable>().Select(variable => variable.Name)
                .ToHashSet(StringComparer.Ordinal);
            var usedFields = bound.Where(pair => argumentNames.Contains(pair.Key) || constructedReads.Contains(pair.Key))
                .OrderBy(pair => pair.Value).ToArray();
            if (usedFields.Length == 0 || first.Arguments.Count + usedFields.Length >= FirstReservedXRegister)
            {
                return null;
            }

            string? lastDirect = usedFields.LastOrDefault(pair => !constructedReads.Contains(pair.Key)).Key;
            var registers = new string[first.Arguments.Count + usedFields.Length + 1];
            foreach (var (name, field) in usedFields)
            {
                int destination = Array.IndexOf(argumentNames, name);
                if (destination < 0)
                {
                    destination = Array.FindIndex(
                        first.Arguments.ToArray(),
                        argument => LocalFunctionEntries.Walk(argument).OfType<Expr.Variable>().Any(read => read.Name == name)
                    );
                }

                if (registers[destination] is not null || destination == 0 && name != lastDirect)
                {
                    destination = Array.FindIndex(registers, name == lastDirect ? 0 : 1, register => register is null);
                }

                registers[destination] = name;
                fields.Add((field, destination));
            }

            if (ReadEntryCall(
                first,
                registers,
                stackInputs,
                allowConstructedArgument: true
            ) is not { } initial)
            {
                return null;
            }

            if (initial.ConstructedArgument is { Register: 0 } constructor
                && fields.Any(field => constructor.Operands.All(operand => operand.Source != field.Register)))
            {
                int scratch = Array.FindIndex(registers, 1, register => register is null);
                initial.Moves.Add((0, scratch));
                initial = initial with { ConstructedArgument = constructor with { Register = scratch } };
            }

            var result = new EntryCall[calls.Length];
            result[0] = initial;
            for (int index = 1; index < calls.Length; index++)
            {
                string? previous = calls[index - 1].Result;
                if (ReadEntryCall(calls[index].Call, previous is null ? [] : [previous], stackInputs) is not { } step)
                {
                    return null;
                }

                result[index] = step;
            }

            return result;
        }

        private sealed record EntrySelector(
            EntryCall Call,
            EntryCall? Prefix,
            int[] Saved,
            string[] StackInputs,
            int Trimmed,
            int[] Expired
        )
        {
            internal int? PrefixResultSlot { get; init; }

            internal bool SavePrefixAfterMoves { get; init; }

            internal (TaggedCallMatch Check, int? Field)? TaggedPrefix { get; init; }

            internal int SavedSlotOffset => TaggedPrefix is not null && PrefixResultSlot is not null ? 1 : 0;

            internal int FrameSize => Math.Max(Saved.Length + SavedSlotOffset, PrefixResultSlot.GetValueOrDefault(-1) + 1);
        }

        private EntrySelector? ReadEntrySelector(
            Expr.Call read,
            string[] parameters,
            int[] survivors,
            bool trimFrame = true,
            (Expr.Call Call, string Result, bool Preserve)? preceding = null,
            (TaggedCallMatch Check, int? Field)? taggedPrefix = null,
            CallListExtraction? extraction = null
        )
        {
            Expr.Call prefixCall;
            Expr.Call selector = read;
            string nestedResult;
            if (preceding is { } bound)
            {
                prefixCall = bound.Call;
                nestedResult = bound.Result;
            }
            else
            {
                var nested = read.Arguments.OfType<Expr.Call>().ToArray();
                if (nested.Length == 0)
                {
                    return ReadEntryCall(read, parameters) is { } direct
                        ? new EntrySelector(
                            direct,
                            null,
                            survivors,
                            survivors.Select(source => parameters[source]).ToArray(),
                            0,
                            []
                        )
                        : null;
                }

                if (nested is not [var inner])
                {
                    return null;
                }

                prefixCall = inner;
                nestedResult = "\0tuple-match-call";
                selector = read with
                {
                    Arguments = read.Arguments.Select(argument => ReferenceEquals(argument, inner)
                        ? new Expr.Variable(nestedResult) : argument).ToArray()
                };
            }

            int[] saved = survivors.Concat(read.Arguments.OfType<Expr.Variable>()
                .Select(variable => Array.IndexOf(parameters, variable.Name)).Where(index => index >= 0))
                .Distinct().OrderByDescending(index => extraction is { } list && index == list.Tail ? list.Head + 1 : index)
                .ToArray();
            string[] stackInputs = saved.Select(source => parameters[source]).ToArray();
            string[] prefixInputs = parameters;
            if (extraction is { } list)
            {
                if (taggedPrefix is not null || !saved.Contains(list.Head) || !saved.Contains(list.Tail))
                {
                    return null;
                }

                prefixInputs = (string[])parameters.Clone();
                prefixInputs[list.Head] = VariableScopeNames.Wildcard;
                prefixInputs[list.Tail] = VariableScopeNames.Wildcard;
            }

            if (ReadEntryCall(
                prefixCall,
                prefixInputs,
                extraction is null ? null : stackInputs
            ) is not { } prefix)
            {
                return null;
            }

            bool saveTaggedField = preceding is { Preserve: true } && taggedPrefix is not null;
            if (saveTaggedField)
            {
                stackInputs = [nestedResult, .. stackInputs];
            }

            if (ReadEntryCall(selector, saveTaggedField ? [] : [nestedResult], stackInputs) is not { } call)
            {
                return null;
            }

            int? resultSlot = null;
            bool savePrefixAfterMoves = false;
            if (saveTaggedField)
            {
                resultSlot = 0;
            }
            else if (preceding is { Preserve: true })
            {
                resultSlot = saved.Length;
                if (call.SavedInputs is { } loads && !call.Moves.Any(move => move.Source == 0))
                {
                    int argument = loads.FindIndex(load => load.Destination == 0);
                    if (argument >= 0)
                    {
                        int slot = loads[argument].Source;
                        if (!survivors.Contains(saved[slot]) && loads.Count(load => load.Source == slot) == 1)
                        {
                            resultSlot = slot;
                            loads.RemoveAt(argument);
                        }
                    }
                }

                if (extraction is not null && resultSlot == saved.Length
                    && call.Moves.All(move => move.Destination != 0)
                    && call.Constants.All(constant => constant.Destination != 0)
                    && (call.SavedInputs?.All(load => load.Destination != 0) ?? true))
                {
                    int reusable = Array.FindLastIndex(saved, source => !survivors.Contains(source));
                    if (reusable >= 0)
                    {
                        resultSlot = reusable;
                        savePrefixAfterMoves = true;
                    }
                }
            }

            int savedSlotOffset = taggedPrefix is not null && resultSlot is not null ? 1 : 0;
            int trimmed = trimFrame && savedSlotOffset == 0
                ? saved.TakeWhile((source, slot) => slot != resultSlot && !survivors.Contains(source)).Count()
                : 0;
            int[] expired = Enumerable.Range(trimmed, saved.Length - trimmed)
                .Where(slot => slot + savedSlotOffset != resultSlot && !survivors.Contains(saved[slot]))
                .Select(slot => slot + savedSlotOffset).ToArray();

            return new EntrySelector(
                call,
                prefix,
                saved,
                stackInputs,
                trimmed,
                expired
            )
            {
                PrefixResultSlot = resultSlot,
                SavePrefixAfterMoves = savePrefixAfterMoves,
                TaggedPrefix = taggedPrefix
            };
        }

        private int EmitEntrySelector(
            EntrySelector selector,
            Expr.Call read,
            string[] parameters,
            (int Tuple, int Field, int Register)? deferred = null,
            int savedOffset = 0,
            int projectionDestination = 0,
            Action<int>? reserveTaggedFailure = null,
            int trailingSlots = 0,
            CallListExtraction? extraction = null,
            (int Field, int Register)[]? savedTupleFields = null
        )
        {
            var (call, prefix, saved, _, trimmed, expired) = selector;
            int live;
            if (extraction is { } list)
            {
                live = ListSequenceLive(
                    prefix ?? call,
                    parameters,
                    saved,
                    list
                );
            }
            else
            {
                live = deferred is { } projection
                    ? Math.Max(
                        projection.Tuple + 1,
                        read.Arguments.Where((_, argument) => argument != projectionDestination).OfType<Expr.Variable>()
                        .Select(variable => Array.IndexOf(parameters, variable.Name) + 1).DefaultIfEmpty(0).Max()
                    )
                    : EntryCallLiveCount(prefix ?? call, parameters);
                live = Math.Max(live, saved.Where(register => savedTupleFields?.Any(field => field.Register == register) != true)
                    .DefaultIfEmpty(-1).Max() + 1);
            }

            EmitFrameAllocation(live, selector.FrameSize + savedOffset + trailingSlots);
            EmitFrameInitialization(0, savedOffset + selector.SavedSlotOffset);
            EmitFrameInitialization(saved.Length + selector.SavedSlotOffset, selector.FrameSize);
            EmitFrameInitialization(selector.FrameSize + savedOffset, selector.FrameSize + savedOffset + trailingSlots);
            for (int slot = 0; slot < saved.Length; slot++)
            {
                int source = saved[slot];
                if ((extraction is not { } parts || source != parts.Head && source != parts.Tail)
                    && savedTupleFields?.Any(field => field.Register == source) != true)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        source,
                        YRegisterTag,
                        slot + savedOffset + selector.SavedSlotOffset
                    );
                }
            }

            if (extraction is { } extracted)
            {
                EmitCallListExtraction(extracted, Array.IndexOf(saved, extracted.Head), Array.IndexOf(saved, extracted.Tail));
            }

            if (prefix is null)
            {
                if (deferred is { } field)
                {
                    owner.EmitTuplePart(field.Tuple, field.Field, projectionDestination);
                    foreach (var (index, register) in savedTupleFields ?? [])
                    {
                        int slot = Array.IndexOf(saved, register) + savedOffset + selector.SavedSlotOffset;
                        owner.Op(GetTupleElement, XRegisterTag, field.Tuple);
                        owner.Operand(BeamWire.UnsignedTag, index);
                        owner.Operand(YRegisterTag, slot);
                    }
                }

                EmitSelectorCall(call);
            }
            else
            {
                EmitSelectorCall(prefix);
                if (selector.TaggedPrefix is { } tagged)
                {
                    reserveTaggedFailure!(checked((int)owner.instructions.Position + 1));
                    EmitDetailRecordTest(
                        0,
                        tagged.Check.Arity,
                        tagged.Check.Tag,
                        0
                    );
                    if (tagged.Field is int fieldIndex)
                    {
                        owner.Op(GetTupleElement, XRegisterTag, 0);
                        owner.Operand(BeamWire.UnsignedTag, fieldIndex);
                        owner.Operand(selector.PrefixResultSlot is null ? XRegisterTag : YRegisterTag, 0);
                    }
                }

                if (!selector.SavePrefixAfterMoves && selector.TaggedPrefix is null && selector.PrefixResultSlot is int result)
                {
                    if (result < saved.Length)
                    {
                        owner.Op(Swap, YRegisterTag, result);
                        owner.Operand(XRegisterTag, 0);
                    }
                    else
                    {
                        owner.MoveRegister(
                            XRegisterTag,
                            0,
                            YRegisterTag,
                            result
                        );
                    }
                }

                owner.EmitOrderedTailMoves(
                    call.Moves,
                    call.Constants,
                    call.SavedInputs,
                    expired
                );
                if (selector.SavePrefixAfterMoves && selector.PrefixResultSlot is int resultAfterMoves)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        resultAfterMoves
                    );
                }

                if (trimmed != 0)
                {
                    owner.Op(TrimFrame, BeamWire.UnsignedTag, trimmed);
                    owner.Operand(BeamWire.UnsignedTag, selector.FrameSize - trimmed);
                }

                owner.EmitCallLine(read, CallLocation(prefix.Call, location));
                Call(call.Module, read.Function, read.Arguments.Count);
            }

            return trimmed;

            void EmitSelectorCall(EntryCall target)
            {
                if (extraction is null)
                {
                    EmitEntryCall(target);

                    return;
                }

                owner.EmitOrderedTailMoves(target.Moves, target.Constants, target.SavedInputs);
                owner.EmitCallLine(target.Call, location);
                Call(target.Module, target.Call.Function, target.Call.Arguments.Count);
            }
        }

        private bool TryEmitRegisterIntegerMembership(FunctionDefinition function)
        {
            if (function.Clauses.Count < 3)
            {
                return false;
            }

            int selector = -1;
            int listRegister = -1;
            Term? result = null;
            var keys = new SortedDictionary<System.Numerics.BigInteger, Integer>();
            foreach (Clause clause in function.Clauses)
            {
                int keyRegister = -1;
                Integer? key = null;
                var bindings = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int index = 0; index < clause.Patterns.Count; index++)
                {
                    switch (clause.Patterns[index])
                    {
                        case Pattern.Literal { Value: Integer integer } when key is null:
                            key = integer;
                            keyRegister = index;
                            break;
                        case Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                            break;
                        case Pattern.Variable variable when bindings.TryAdd(variable.Name, index):
                            break;
                        default:
                            return false;
                    }
                }

                int guarded = -1;
                if (clause.Guard is not null)
                {
                    if (clause.Guard is not Expr.Call
                        {
                            Module: null or ErlangModuleNames.Module, Function: AutoImportNames.IsList,
                            DynamicRemote: false, Arguments: [Expr.Variable variable]
                        } guard || guard.Module is null && entries.ContainsKey((guard.Function, 1))
                        || !bindings.TryGetValue(variable.Name, out guarded))
                    {
                        return false;
                    }
                }

                if (key is null || result is not null && (selector != keyRegister || listRegister != guarded)
                    || !AttributeLiteral.TryNormalizeConstant(clause.Body, out Term value)
                    || result is not null && !result.Equals(value) || !keys.TryAdd(key.Value, key))
                {
                    return false;
                }

                selector = keyRegister;
                listRegister = guarded;
                result = value;
            }

            int functionInfo = owner.nextLabel - 2;
            int matched = Label();
            owner.EmitMembershipSelect(selector, functionInfo, keys.Values.Select(key => ((Term)key, matched)));
            Mark(matched);
            if (listRegister >= 0)
            {
                owner.Op(IsList, LabelTag, functionInfo);
                owner.Operand(XRegisterTag, listRegister);
            }
            owner.EmitConstant(result!, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private bool TryEmitNestedTaggedForward(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null,
                    Patterns: [Pattern.Variable first, AliasPattern { Left: Pattern.Tuple outer, Right: Pattern.Variable input }],
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [Expr.Variable passed, Expr.Variable projection, Expr.Variable original]
                    } call
                }] || !DistinctBindings(first, input) || passed.Name != first.Name || original.Name != input.Name
                || projection.Name == first.Name || projection.Name == input.Name || projection.Name == VariableScopeNames.Wildcard
                || !entries.TryGetValue((call.Function, 3), out int entry)
                || ReadSingleTupleField(outer) is not { Field: Pattern.Tuple inner } parent
                || ReadSingleTupleField(inner) is not { Field: Pattern.Variable member } child
                || member.Name != projection.Name)
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            EmitCheck(1, outer.Items.Count, parent.Tag);
            owner.EmitTuplePart(1, parent.Index, 2);
            EmitCheck(2, inner.Items.Count, child.Tag);
            owner.EmitTuplePart(2, child.Index, 2);
            owner.Op(Swap, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 2);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 3);
            owner.Operand(LabelTag, entry);

            return true;

            void EmitCheck(int register, int arity, Atom tag)
            {
                owner.Op(IsTaggedTuple, LabelTag, failure);
                owner.Operand(XRegisterTag, register);
                owner.Operand(BeamWire.UnsignedTag, arity);
                owner.Operand(AtomTag, owner.Intern(tag.Name));
            }
        }

        private static (Atom Tag, int Index, Pattern Field)? ReadSingleTupleField(Pattern.Tuple tuple)
        {
            if (tuple.Items.Count < 2 || tuple.Items[0] is not Pattern.Literal { Value: Atom tag })
            {
                return null;
            }

            var fields = tuple.Items.Skip(1).Select((field, index) => (field, index))
                .Where(entry => entry.field is not Pattern.Variable { Name: VariableScopeNames.Wildcard }).ToArray();

            return fields is [var field] ? (tag, field.index + 1, field.field) : null;
        }
    }
}
