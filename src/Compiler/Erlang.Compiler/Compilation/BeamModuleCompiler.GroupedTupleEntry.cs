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
            if (input < 0)
            {
                return false;
            }

            var groups = tupleClauses
                .GroupBy(clause => ((Pattern.Tuple)EntryShape(clause.Patterns[input])).Items.Count)
                .OrderBy(group => group.Key).ToArray();
            if (groups.Length < 2)
            {
                return false;
            }

            if (input == 0 && (TryEmitGroupedTupleProjections(function, groups, fallback, functionInfo)
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
            owner.EmitTupleArityDispatch(noMatch, cases, input);

            int argumentBase = nextSlot;
            int[] arguments = Enumerable.Range(0, function.Arity).Select(_ => Slot()).ToArray();
            int clauseBase = nextSlot;
            int? failed = null;
            int functionFrame = frameSize;
            int functionPeak = peakSlot;
            var frames = new Dictionary<int, int>();
            for (int groupIndex = groups.Length - 1; groupIndex >= 0; groupIndex--)
            {
                Mark(cases[groupIndex].Label);
                var group = groups[groupIndex];
                var clauses = group.ToArray();
                peakSlot = argumentBase;
                frameSize = plannedTupleFrames?.GetValueOrDefault(group.Key, functionFrame) ?? functionFrame;
                if (clauses is [var single] && TryEmitTupleEntryWithRegisterFields(
                    single,
                    noMatch,
                    argumentBase,
                    groupedInput: useEntryScratch ? input : null
                ))
                {
                    frames.Add(group.Key, peakSlot);
                    functionPeak = Math.Max(functionPeak, peakSlot);
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

                for (int clauseIndex = 0; clauseIndex < clauses.Length; clauseIndex++)
                {
                    Clause clause = clauses[clauseIndex];
                    int failure = clauseIndex == clauses.Length - 1 ? failed.Value : Label();
                    nextSlot = clauseBase;
                    variables = new(StringComparer.Ordinal);
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

                    Guard(clause.Guard, failure);
                    if (!TailLocal(clause.Body))
                    {
                        Expression(clause.Body);
                        owner.EmitFrameReturn(frameSize);
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
                if (fallback is null)
                {
                    JumpTo(functionInfo);
                }
            }

            if (fallback is not null)
            {
                Mark(noMatch);
                EmitGroupedTupleFallback(fallback, frames);
            }

            return true;
        }

        private void EmitGroupedTupleFallback(Clause clause, Dictionary<int, int> frames)
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

            var used = ReadUsedEntryParameters(clause);
            var inputs = parameters.Select((name, register) => (Variable: new Pattern.Variable(name), Register: register))
                .Where(binding => binding.Variable.Name != VariableScopeNames.Wildcard
                    && (used is null || used.Contains(binding.Variable.Name))).ToArray();
            EmitEntryClauseBody(
                clause,
                inputs,
                TupleFallbackFrame,
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
                Pattern.Tuple { } => TryEmitTupleEntryWithRegisterFields(first, failure, baseSlot: null, reserveFailure: ReserveFailure)
                    || TryEmitTupleEntryWithRegisterFields(first, failure, nextSlot, ReserveFailure, checkShape: true)
                    || TryEmitPlannedTupleEntry(first, failure, ReserveFailure),
                Pattern.List { } when first.Body is Expr.Call { DynamicRemote: false } =>
                    TryEmitConsEntryInRegisters(first, 0, failure, EmitListTest),
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
                    owner.MoveRegister(XRegisterTag, result, XRegisterTag, 0);
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

            EmitDetailRecordTest(0, record.Items.Count, tag, failure);
            EmitFrameAllocation(2, 2);
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 0);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 1);
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

            owner.MoveRegister(YRegisterTag, 0, YRegisterTag, 1);
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
            if (!TryEmitPlannedTupleEntry(left, failure, checkedRootFields: sharedFields, validateOnly: true)
                || !TryEmitPlannedTupleEntry(right, failure, checkedRootFields: sharedFields, validateOnly: true))
            {
                return false;
            }

            if (TermOrder.Compare(leftKey!, rightKey!, exact: true) > 0)
            {
                (leftKey, rightKey) = (rightKey, leftKey);
                (left, right) = (right, left);
            }

            int register = function.Arity;
            EmitDetailRecordTest(0, first.Items.Count, tag, failure);
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
            owner.EmitAtomSelection(register, failure, leftKey!.Name, leftLabel, rightKey!.Name, rightLabel);
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

            EmitTupleShapeTest(XRegisterTag, 0, fields.Length, failure);
            EmitEntryCallChain(first, parameters, continuation, relocateBeforeTrim: true, entryTupleArity: fields.Length);
            return true;
        }

        private bool TryEmitPlannedTupleEntry(
            Clause clause,
            int failure,
            Action? reserveFailure = null,
            HashSet<int>? checkedRootFields = null,
            bool validateOnly = false
        )
        {
            if (clause.Patterns[0] is not Pattern.Tuple root
                || reserveFailure is not null && checkedRootFields is null && clause.Guard is null && !root.Items.Any(item => item is Pattern.Tuple))
            {
                return false;
            }

            int arity = clause.Patterns.Count;
            int nextRegister = arity;
            var checks = new List<Action>();
            var bindings = new Dictionary<string, (int Input, int? Field)>(StringComparer.Ordinal);
            for (int index = 1; index < arity; index++)
            {
                if (clause.Patterns[index] is not Pattern.Variable parameter || !AddBinding(parameter, index, null))
                {
                    return false;
                }
            }

            if (!PlanTuple(root, 0))
            {
                return false;
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

            reserveFailure?.Invoke();
            foreach (Action check in checks)
            {
                check();
            }

            var saved = bindings.Where(binding => used is null || used.Contains(binding.Key)).ToArray();
            Expr.Call? forwardedCall = null;
            (int Input, int Field) forwardedField = default;
            (int? Slot, Term? Constant)[]? forwardedOperands = null;
            if (clause.Body is Expr.Sequence { Items: [
                Expr.Match
                {
                    Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } result,
                    Value: Expr.Call { Module: not null and not ErlangModuleNames.Module, DynamicRemote: false,
                        Arguments: [Expr.Variable argument] } call
                },
                Expr.Tuple resultTuple
            ] } && !bindings.ContainsKey(result.Name)
                && bindings.TryGetValue(argument.Name, out var source) && source.Field is int fieldIndex)
            {
                var surviving = saved.Where(binding => binding.Key != argument.Name).ToArray();
                var slots = surviving.Select((binding, index) => (binding.Key, Slot: nextSlot + surviving.Length - 1 - index))
                    .ToDictionary(binding => binding.Key, binding => binding.Slot, StringComparer.Ordinal);
                if (TryReadBoundTupleOperands(resultTuple, result.Name, slots, out var operands))
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
                    owner.MoveRegister(XRegisterTag, input, YRegisterTag, slot);
                }

                variables.Add(name, slot);
            }

            if (forwardedCall is not null)
            {
                owner.EmitTuplePart(forwardedField.Input, forwardedField.Field, 0);
                owner.EmitCallLine(forwardedCall, location);
                Call(forwardedCall.Module, forwardedCall.Function, 1);
                EmitTupleWithX0(forwardedOperands!);
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

            return true;

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

            bool PlanTuple(Pattern.Tuple tuple, int input)
            {
                bool tagged = HasTupleTag(tuple, out Atom tag);
                if (input != 0 || checkedRootFields is null)
                {
                    checks.Add(tagged
                        ? () => EmitDetailRecordTest(input, tuple.Items.Count, tag, failure)
                        : () => EmitTupleShapeTest(XRegisterTag, input, tuple.Items.Count, failure));
                }
                for (int index = tagged ? 1 : 0; index < tuple.Items.Count; index++)
                {
                    if (input == 0 && checkedRootFields?.Contains(index) == true)
                    {
                        continue;
                    }

                    int field = index;
                    int scratch = nextRegister;
                    switch (tuple.Items[index])
                    {
                        case Pattern.Any { }:
                            break;
                        case Pattern.Variable variable when AddBinding(variable, input, field):
                            break;
                        case Pattern.Literal literal:
                            checks.Add(() =>
                            {
                                owner.EmitTuplePart(input, field, scratch);
                                owner.Op(ExactEqualTest, LabelTag, failure);
                                owner.Operand(XRegisterTag, scratch);
                                owner.LiteralOperand(literal.Value);
                            });
                            break;
                        case Pattern.Tuple nested:
                            nextRegister++;
                            checks.Add(() => owner.EmitTuplePart(input, field, scratch));
                            if (!PlanTuple(nested, scratch))
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

        private bool HasProvenEntryTupleShape(FunctionDefinition function, int input, int arity, Atom? tag)
        {
            return !owner.IsDeclaredNative((function.Name, function.Arity))
                && HasOnlyLocalCalls(function, function.Arity, (caller, invocation) =>
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

                    return argument is Expr.Variable variable && caller.Clauses.Any(clause =>
                        clause.Patterns.Any(pattern => pattern is AliasPattern
                        {
                            Left: Pattern.Tuple matched, Right: Pattern.Variable alias
                        } && alias.Name == variable.Name && matched.Items.Count == arity
                            && (tag is null || matched.Items[0] is Pattern.Literal { Value: Atom value } && value.Equals(tag)))
                        && HasUnshadowedInvocation(clause, invocation));
                });
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
                        && HasProvenEntryTupleShape(function, tupleInput, tuple.Items.Count, tag);
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
                if (TryEmitRegisterConstructedArgument(constructedCall, registers, (clause.Patterns.Count, EmitFields), EmitTests))
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
                EmitTests();
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
                    owner.MoveRegister(XRegisterTag, register, YRegisterTag, slot);
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

            void EmitTests()
            {
                reserveFailure?.Invoke();
                if (needsShape)
                {
                    if (tag is not null)
                    {
                        EmitDetailRecordTest(tupleInput, tuple.Items.Count, tag, failure);
                    }
                    else
                    {
                        EmitTupleShapeTest(XRegisterTag, tupleInput, tuple.Items.Count, failure);
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
