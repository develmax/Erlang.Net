using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitDynamicUnicodePair(FunctionDefinition function, int failure)
        {
            if (function.Clauses is not [{
                Patterns: [Pattern.Variable first, Pattern.Variable second],
                Guard: Expr.Binary { Operator: ErlangOperators.AndAlso, Left: var firstGuard, Right: var secondGuard },
                Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.Variable module, Value: Expr.Call { Arguments.Count: 0 } producer },
                    var firstBinding, var secondBinding,
                    Expr.Call { DynamicRemote: true, Arguments: [Expr.Variable target, Expr.Literal { Value: Atom member },
                        Expr.List { Items: [Expr.Variable firstArg, Expr.Variable secondArg, var lastArg], Tail: null }] } apply,
                    var result
                ] }
            } clause] || !HasUnconditionalParameters(clause) || target.Name != module.Name
                || ReadEntryTypeGuard(clause, firstGuard) != (IsList, 0)
                || ReadEntryTypeGuard(clause, secondGuard) != (IsList, 1)
                || !TryResolveOrdinaryCall(producer, out _)
                || ReadConversion(firstBinding, first.Name, firstArg.Name) is not { } left
                || ReadConversion(secondBinding, second.Name, secondArg.Name) is not { } right
                || new[] { first.Name, second.Name, module.Name, firstArg.Name, secondArg.Name }
                    .Distinct(StringComparer.Ordinal).Count() != 5
                || new[] { module.Name, firstArg.Name, secondArg.Name }.Contains(VariableScopeNames.Wildcard)
                || !AttributeLiteral.TryNormalizeConstant(lastArg, out _)
                || ReadEntryCaseExternalTail(result) is not { Call.Arguments: [var tailArgument] } tail
                || !AttributeLiteral.TryNormalizeConstant(tailArgument, out _))
            {
                return false;
            }

            EmitEntryGuardPrefix(clause, clause.Guard, failure);
            EmitFrameAllocation(2, 2);
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 0);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 1);
            Expression(producer);
            EmitConversion(left.Call, left.Suffix, 1);
            EmitConversion(right.Call, right.Suffix, 0);
            owner.EmitConstant(lastArg, 2);
            Load(1, 3);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
            owner.EmitConstant(member, 4);
            Load(0);
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitCallLine(apply, location);
            owner.Op(DynamicApply, BeamWire.UnsignedTag, 3);
            owner.EmitConstant(tailArgument, 0);
            owner.EmitCallLine(tail.Call, location);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(tail.Module, tail.Call.Function, 1));
            owner.Operand(BeamWire.UnsignedTag, 0);
            return true;

            static (Expr.Call Call, Term Suffix)? ReadConversion(Expr binding, string input, string output) =>
                binding is Expr.Match { Pattern: Pattern.Variable converted, Value: Expr.Call
                    { Module: "unicode", Function: "characters_to_binary", DynamicRemote: false, Arguments: [var value] } call }
                && converted.Name == output && ReadUnicodeMappingInput(value) is { Suffix: { } suffix } mapped
                && mapped.Name == input ? (call, suffix) : null;

            void EmitConversion(Expr.Call call, Term suffix, int slot)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.Op(PutList, YRegisterTag, slot);
                owner.LiteralOperand(suffix);
                owner.Operand(XRegisterTag, 1);
                owner.MoveRegister(XRegisterTag, 0, YRegisterTag, slot);
                owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
                owner.EmitCallLine(call, location);
                Call(call.Module, call.Function, 1);
            }
        }

        private bool TryEmitConstantRemoteCall(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Body: Expr.Sequence sequence } clause]
                || sequence.Items.Count is not (3 or 4 or 5)
                || sequence.Items[0] is not Expr.Match { Pattern: Pattern.Variable binding, Value: Expr.Literal { Value: Atom module } }
                || sequence.Items[^2] is not Expr.Call { DynamicRemote: true, Arguments: [
                    Expr.Variable target, Expr.Literal { Value: Atom method }, Expr.List { Tail: null } operands
                ] } dynamicCall || !HasUnconditionalParameters(clause)
                || binding.Name == VariableScopeNames.Wildcard || binding.Name != target.Name
                || !TryEmitConstantRemoteGuard(clause, clause.Guard, owner.nextLabel - 2, validateOnly: true))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            var length = sequence.Items.Count == 4 ? ReadConstantRemoteLength(sequence.Items[1], clause.Guard, parameters, operands.Items) : null;
            var unicode = sequence.Items.Count == 4 && length is null
                ? ReadConstantRemoteUnicode(sequence.Items[1], parameters, operands.Items) : null;
            var mapping = sequence.Items.Count == 5
                ? ReadConstantRemoteMapping(sequence, clause.Guard, parameters, operands.Items) : null;
            string? computedName = length?.Name ?? unicode?.Name;
            if (sequence.Items.Count == 4 && (computedName is null || computedName == binding.Name)
                || sequence.Items.Count == 5 && (mapping is not { } plan
                    || plan.MappedName == binding.Name || plan.CountName == binding.Name))
            {
                return false;
            }

            var staticCall = dynamicCall with
            {
                Module = module.Name,
                Function = method.Name,
                Arguments = operands.Items,
                DynamicRemote = false
            };
            string[] inputs = mapping is { } mapped ? [.. parameters, mapped.MappedName, mapped.CountName]
                : computedName is null ? parameters : [.. parameters, computedName];
            if (parameters.Contains(binding.Name, StringComparer.Ordinal)
                || ReadEntryCall(staticCall, inputs)
                    is not { Module: not null } call)
            {
                return false;
            }

            Expr result = sequence.Items[^1];
            bool constantResult = AttributeLiteral.TryNormalizeConstant(result, out Term constant);
            var tail = result is Expr.Call { Module: null, DynamicRemote: false } local
                && entries.ContainsKey((local.Function, local.Arguments.Count))
                ? ReadEntryCall(local, []) : null;
            if (!constantResult && tail is null)
            {
                return false;
            }

            TryEmitConstantRemoteGuard(clause, clause.Guard, owner.nextLabel - 2);
            if (length is { } measured)
            {
                owner.EmitCallLine(measured.Call, location);
                EmitSizeInstruction(measured.Call.Function, 0, function.Arity);
                owner.EmitTypedRegister(measured.Source, ConsTypeBits | (1 << 7));
                owner.Operand(XRegisterTag, function.Arity);
                EmitFrameAllocation(function.Arity + 1, 0);
                owner.EmitArgumentMoves([], call.Constants);
                for (int register = function.Arity; register > measured.Destination; register--)
                {
                    owner.Op(Swap, XRegisterTag, register - 1);
                    owner.Operand(XRegisterTag, register);
                }

                owner.EmitCallLine(call.Call, CallLocation(measured.Call, location));
                Call(call.Module, call.Call.Function, call.Call.Arguments.Count);
            }
            else if (unicode is { } converted)
            {
                EmitConstantRemoteUnicode(converted.Call, converted.Suffix, call, function.Arity);
            }
            else if (mapping is { } map)
            {
                EmitConstantRemoteMapping(map.Map, map.Entry, map.Length, call, function.Arity);
            }
            else
            {
                EmitFrameAllocation(function.Arity, 0);
                EmitEntryCall(call);
            }
            if (tail is not null)
            {
                owner.EmitOrderedTailMoves(tail.Moves, tail.Constants);
                owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, tail.Call.Arguments.Count);
                owner.Operand(LabelTag, entries[(tail.Call.Function, tail.Call.Arguments.Count)]);
                owner.Operand(BeamWire.UnsignedTag, 0);
            }
            else
            {
                owner.EmitConstant(constant, 0);
                owner.EmitFrameReturn(0);
            }

            return true;
        }

        private (string MappedName, string CountName, Expr.Call Map, int Entry, Expr.Call Length)? ReadConstantRemoteMapping(
            Expr.Sequence sequence, Expr? guard, string[] parameters, IReadOnlyList<Expr> operands)
        {
            if (parameters.Length is not (2 or 3) || operands.Count < parameters.Length + 1
                || sequence.Items[1] is not Expr.Match { Pattern: Pattern.Variable mapped, Value: Expr.Call
                    { Module: null, DynamicRemote: false, Arguments: [Expr.Variable source] } map }
                || sequence.Items[2] is not Expr.Match { Pattern: Pattern.Variable count, Value: Expr.Call
                    { Arguments: [Expr.Variable list] } length }
                || source.Name != parameters[1] || list.Name != source.Name || mapped.Name == count.Name
                || mapped.Name == VariableScopeNames.Wildcard || count.Name == VariableScopeNames.Wildcard
                || parameters.Contains(mapped.Name, StringComparer.Ordinal) || parameters.Contains(count.Name, StringComparer.Ordinal)
                || !entries.TryGetValue((map.Function, 1), out int entry)
                || !localFunctions.TryGetValue((map.Function, 1), out var helper)
                || ReadClosedLocalMapping(helper) is not { Suffix: not null }
                || !IsGuardBif(length, AutoImportNames.Length) || !owner.ResolvesErlangBif(length)
                || !HasConstantRemoteListGuard(guard, source.Name))
            {
                return null;
            }

            var names = operands.Take(parameters.Length + 1).OfType<Expr.Variable>().Select(variable => variable.Name);
            return names.SequenceEqual([parameters[0], count.Name, mapped.Name, .. parameters.Skip(2)], StringComparer.Ordinal)
                && operands.Skip(parameters.Length + 1).All(operand => AttributeLiteral.TryNormalizeConstant(operand, out _))
                ? (mapped.Name, count.Name, map, entry, length) : null;
        }

        private void EmitConstantRemoteMapping(Expr.Call map, int entry, Expr.Call length, EntryCall call, int arity)
        {
            EmitFrameAllocation(arity, arity);
            for (int register = arity - 1; register >= 0; register--)
            {
                owner.MoveRegister(XRegisterTag, register, YRegisterTag, arity - 1 - register);
            }

            owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
            owner.EmitCallLine(map, location);
            owner.EmitFieldCall(1, entry);
            owner.EmitCallLine(length, CallLocation(map, location));
            EmitSizeInstruction(length.Function, 0, 1);
            owner.EmitTypedRegister(arity - 2, ConsTypeBits | (1 << 7), registerTag: YRegisterTag);
            owner.Operand(XRegisterTag, 1);
            if (arity == 3)
            {
                owner.MoveRegister(YRegisterTag, 0, XRegisterTag, 3);
            }

            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 2);
            owner.EmitArgumentMoves([], call.Constants);
            owner.MoveRegister(YRegisterTag, arity - 1, XRegisterTag, 0);
            owner.Op(TrimFrame, BeamWire.UnsignedTag, arity);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitCallLine(call.Call, CallLocation(length, location));
            Call(call.Module, call.Call.Function, call.Call.Arguments.Count);
        }

        private static (string Name, Expr.Call Call, Term Suffix)? ReadConstantRemoteUnicode(
            Expr expression, string[] parameters, IReadOnlyList<Expr> operands)
        {
            if (parameters.Length == 0 || operands.Count < parameters.Length
                || expression is not Expr.Match { Pattern: Pattern.Variable binding, Value: Expr.Call
                    { Module: "unicode", Function: "characters_to_binary", DynamicRemote: false, Arguments: [var input] } call }
                || binding.Name == VariableScopeNames.Wildcard || parameters.Contains(binding.Name, StringComparer.Ordinal)
                || ReadUnicodeMappingInput(input) is not { Suffix: { } suffix } mapped || mapped.Name != parameters[^1])
            {
                return null;
            }

            var names = operands.Take(parameters.Length).OfType<Expr.Variable>().Select(variable => variable.Name);
            return names.SequenceEqual([.. parameters.SkipLast(1), binding.Name], StringComparer.Ordinal)
                && operands.Skip(parameters.Length).All(operand => AttributeLiteral.TryNormalizeConstant(operand, out _))
                ? (binding.Name, call, suffix) : null;
        }

        private void EmitConstantRemoteUnicode(Expr.Call conversion, Term suffix, EntryCall call, int arity)
        {
            int saved = arity - 1;
            EmitFrameAllocation(arity, saved, heapWords: ConsHeapWords);
            for (int register = saved - 1; register >= 0; register--)
            {
                owner.MoveRegister(XRegisterTag, register, YRegisterTag, saved - 1 - register);
            }

            owner.Op(PutList, XRegisterTag, saved);
            owner.LiteralOperand(suffix);
            owner.Operand(XRegisterTag, 0);
            owner.EmitCallLine(conversion, location);
            Call(conversion.Module, conversion.Function, 1);
            for (int register = saved - 1; register >= 2; register--)
            {
                owner.MoveRegister(YRegisterTag, saved - 1 - register, XRegisterTag, register);
            }

            if (saved > 0)
            {
                owner.MoveRegister(XRegisterTag, 0, XRegisterTag, saved);
            }

            if (saved > 1)
            {
                owner.MoveRegister(YRegisterTag, saved - 2, XRegisterTag, 1);
            }

            owner.EmitArgumentMoves([], call.Constants);
            if (saved > 0)
            {
                owner.MoveRegister(YRegisterTag, saved - 1, XRegisterTag, 0);
                owner.Op(TrimFrame, BeamWire.UnsignedTag, saved);
                owner.Operand(BeamWire.UnsignedTag, 0);
            }

            owner.EmitCallLine(call.Call, CallLocation(conversion, location));
            Call(call.Module, call.Call.Function, call.Call.Arguments.Count);
        }

        private (string Name, Expr.Call Call, int Source, int Destination)? ReadConstantRemoteLength(
            Expr expression, Expr? guard, string[] parameters, IReadOnlyList<Expr> operands)
        {
            if (expression is not Expr.Match { Pattern: Pattern.Variable binding, Value: Expr.Call
                { Arguments: [Expr.Variable list] } length }
                || binding.Name == VariableScopeNames.Wildcard || parameters.Contains(binding.Name, StringComparer.Ordinal)
                || !IsGuardBif(length, AutoImportNames.Length) || !owner.ResolvesErlangBif(length)
                || !HasConstantRemoteListGuard(guard, list.Name) || operands.Count <= parameters.Length)
            {
                return null;
            }

            int source = Array.IndexOf(parameters, list.Name);
            var names = operands.Take(parameters.Length + 1).OfType<Expr.Variable>().Select(variable => variable.Name).ToArray();
            int destination = Array.IndexOf(names, binding.Name);
            if (source < 0 || names.Length != parameters.Length + 1 || destination < 0
                || !names.Where((_, index) => index != destination).SequenceEqual(parameters, StringComparer.Ordinal)
                || !operands.Skip(parameters.Length + 1).All(operand => AttributeLiteral.TryNormalizeConstant(operand, out _)))
            {
                return null;
            }

            return (binding.Name, length, source, destination);

        }

        private bool HasConstantRemoteListGuard(Expr? guard, string name) =>
            guard is Expr.Binary { Operator: ErlangOperators.AndAlso } pair
                ? HasConstantRemoteListGuard(pair.Left, name) || HasConstantRemoteListGuard(pair.Right, name)
                : guard is Expr.Call call && IsListGuard(call, name);

        private bool TryEmitConstantRemoteGuard(Clause clause, Expr? guard, int failure, bool validateOnly = false)
        {
            if (guard is null)
            {
                return true;
            }

            if (guard is Expr.GuardAlternatives { Items: [var firstAlternative, var secondAlternative] })
            {
                guard = new Expr.Binary(ErlangOperators.OrElse, firstAlternative, secondAlternative);
            }

            if (guard is Expr.Binary { Operator: ErlangOperators.AndAlso } conjunction)
            {
                return TryEmitConstantRemoteGuard(clause, conjunction.Left, failure, validateOnly)
                    && TryEmitConstantRemoteGuard(clause, conjunction.Right, failure, validateOnly);
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (ReadTupleArityGuard(guard) is { Value: Expr.Variable tuple, Arity: var arity }
                && Array.IndexOf(parameters, tuple.Name) is int tupleRegister && tupleRegister >= 0)
            {
                if (!validateOnly)
                {
                    EmitTupleShapeTest(XRegisterTag, tupleRegister, arity, failure);
                }

                return true;
            }

            if (guard is Expr.Binary { Operator: ErlangOperators.OrElse } disjunction
                && ReadIntegerTest(disjunction.Left) is { } left
                && ReadIntegerTest(disjunction.Right) is { } right
                && left.Name == right.Name && !left.Value.Equals(right.Value)
                && Array.IndexOf(parameters, left.Name) is int register && register >= 0)
            {
                if (!validateOnly)
                {
                    int success = Label();
                    owner.Op(SelectValue, XRegisterTag, register);
                    owner.Operand(LabelTag, failure);
                    owner.instructions.WriteByte(ExtendedListOperand);
                    owner.Operand(BeamWire.UnsignedTag, 4);
                    foreach (Integer value in new[] { left.Value, right.Value }.OrderBy(value => value.Value))
                    {
                        owner.LiteralOperand(value);
                        owner.Operand(LabelTag, success);
                    }

                    Mark(success);
                }

                return true;
            }

            if (guard is Expr.Binary { Operator: ErlangOperators.OrElse } arities
                && ReadTupleArityGuard(arities.Left) is { Value: Expr.Variable first, Arity: var firstSize }
                && ReadTupleArityGuard(arities.Right) is { Value: Expr.Variable second, Arity: var secondSize }
                && first.Name == second.Name && firstSize != secondSize
                && Array.IndexOf(parameters, first.Name) is int arityRegister && arityRegister >= 0)
            {
                if (!validateOnly)
                {
                    int alternative = Label();
                    int success = Label();
                    owner.Op(IsTuple, LabelTag, failure);
                    owner.Operand(XRegisterTag, arityRegister);
                    TestArityAt(firstSize, alternative);
                    JumpTo(success);
                    Mark(alternative);
                    TestArityAt(secondSize, failure);
                    Mark(success);
                }

                return true;

                void TestArityAt(int size, int label)
                {
                    owner.Op(TestArity, LabelTag, label);
                    owner.Operand(XRegisterTag, arityRegister);
                    owner.Operand(BeamWire.UnsignedTag, size);
                }
            }

            if (guard is Expr.Binary { Operator: ErlangOperators.OrElse })
            {
                var tests = new List<(byte Opcode, int Register)>();
                if (ReadTypeAlternatives(guard, tests)
                    && tests.All(test => test.Register == tests[0].Register))
                {
                    if (!validateOnly)
                    {
                        int[] alternatives = tests.Skip(1).Select(_ => Label()).ToArray();
                        int success = Label();
                        for (int index = 0; index < tests.Count; index++)
                        {
                            bool last = index == tests.Count - 1;
                            owner.Op(tests[index].Opcode, LabelTag, last ? failure : alternatives[index]);
                            owner.Operand(XRegisterTag, tests[index].Register);
                            if (!last)
                            {
                                JumpTo(success);
                                Mark(alternatives[index]);
                            }
                        }

                        Mark(success);
                    }

                    return true;
                }
            }

            if (!CanEmitSeparateEntryGuard(clause, guard))
            {
                return false;
            }

            if (!validateOnly)
            {
                EmitEntryGuardPrefix(clause, guard, failure);
            }

            return true;

            bool ReadTypeAlternatives(Expr expression, List<(byte Opcode, int Register)> tests)
            {
                if (expression is Expr.Binary { Operator: ErlangOperators.OrElse } pair)
                {
                    return ReadTypeAlternatives(pair.Left, tests) && ReadTypeAlternatives(pair.Right, tests);
                }

                if (ReadEntryTypeGuard(clause, expression) is not { } test)
                {
                    return false;
                }

                tests.Add(test);
                return true;
            }

            static (string Name, Integer Value)? ReadIntegerTest(Expr expression)
            {
                if (expression is not Expr.Binary { Operator: ErlangOperators.ExactEqual } test)
                {
                    return null;
                }

                var (variable, constant) = test.Left is Expr.Variable
                    ? (test.Left, test.Right) : (test.Right, test.Left);
                return variable is Expr.Variable name && constant is Expr.Literal { Value: Integer value }
                    && value.Value >= int.MinValue && value.Value <= int.MaxValue ? (name.Name, value) : null;
            }
        }
    }
}
