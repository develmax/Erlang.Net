using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record OrderedPairUpdateArm(
            Pattern.Variable[] Parameters,
            Pattern.Variable Index,
            Pattern.Variable Value,
            Pattern.Variable Tail,
            string? ListAlias,
            string? HeadAlias
        );

        private bool TryEmitOrderedPairUpdate(FunctionDefinition function, int functionInfo)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Arity is not (3 or 4) || function.Clauses is not
                    [var insert, var continueScan, { Guard: null } matched, { Guard: null } empty])
            {
                return false;
            }

            int input = function.Arity - 1;
            if (ReadOrderedPairUpdateArm(insert, input) is not { ListAlias: { } whole } before
                || ReadOrderedPairUpdateArm(continueScan, input) is not { HeadAlias: { } pair } scan
                || ReadOrderedPairUpdateArm(matched, input) is not { } equal
                || !HasLookupComparison(
                    insert.Guard,
                    ErlangOperators.Less,
                    before.Parameters[0],
                    before.Index
                )
                || !HasLookupComparison(
                    continueScan.Guard,
                    ErlangOperators.Greater,
                    scan.Parameters[0],
                    scan.Index
                )
                || insert.Body is not Expr.List { Items: [Expr.Tuple first], Tail: Expr.Variable original }
                || original.Name != whole
                || ReadOrderedPairUpdateValue(first, before.Parameters) is not { } seed
                || continueScan.Body is not Expr.List
                {
                    Items: [Expr.Variable retained],
                    Tail: Expr.Call { Module: null, DynamicRemote: false } recurse
                }
                || retained.Name != pair || recurse.Function != function.Name || recurse.Arguments.Count != function.Arity
                || !recurse.Arguments.Take(input).Select((argument, index) =>
                    argument is Expr.Variable variable && variable.Name == scan.Parameters[index].Name).All(matches => matches)
                || recurse.Arguments[^1] is not Expr.Variable nextTail || nextTail.Name != scan.Tail.Name
                || !entries.ContainsKey((recurse.Function, recurse.Arguments.Count))
                || matched.Body is not Expr.List
                {
                    Items: [Expr.Tuple { Items: [Expr.Variable key, var value] }],
                    Tail: Expr.Variable matchedTail
                }
                || key.Name != equal.Parameters[0].Name || matchedTail.Name != equal.Tail.Name
                || ListEntryTest(empty.Patterns[input]) != IsNil
                || empty.Patterns.Take(input).Any(pattern => pattern is not global::Erlang.Pattern.Variable)
                || empty.Body is not Expr.List { Items: [Expr.Tuple singleton], Tail: null })
            {
                return false;
            }

            var emptyInputs = empty.Patterns.Take(input).Cast<Pattern.Variable>().ToArray();
            if (!DistinctBindings(emptyInputs.Where(binding => binding.Name != VariableScopeNames.Wildcard).ToArray())
                || ReadOrderedPairUpdateValue(singleton, emptyInputs) != seed)
            {
                return false;
            }

            Expr? update = null;
            if (ReadOrderedPairUpdateValue(new Expr.Tuple([key, value]), equal.Parameters) != seed)
            {
                if (function.Arity == 3 && seed.Register == 1
                    && value is Expr.Binary
                    {
                        Operator: ErlangOperators.Append or ErlangOperators.Plus,
                        Left: Expr.Variable old
                    } binary
                    && old.Name == equal.Value.Name && old.Name != VariableScopeNames.Wildcard
                    && ReadOrderedPairSeed(binary.Right, equal.Parameters) == seed
                    && (binary.Operator == ErlangOperators.Append || !seed.Singleton))
                {
                    update = binary;
                }
                else if (function.Arity == 4 && seed is { Register: 2, Singleton: false }
                    && value is Expr.Apply
                    {
                        Function: Expr.Variable callback,
                        Arguments: [Expr.Variable operand]
                    } apply
                    && callback.Name == equal.Parameters[1].Name
                    && operand.Name == equal.Value.Name && operand.Name != VariableScopeNames.Wildcard)
                {
                    update = apply;
                }
                else
                {
                    return false;
                }
            }

            int head = function.Arity;
            int tail = head + 1;
            int indexRegister = tail + 1;
            int sharedLive = tail + 1;
            int next = Label();
            int replace = Label();
            int nonCons = Label();

            EmitOrderedPairUpdateEntry(function.Arity, nonCons, functionInfo);
            EmitOrderedPairLess(0, indexRegister, next);
            EmitSeedReturn(input, function.Arity);

            Mark(next);
            EmitOrderedPairLess(indexRegister, 0, replace);
            EmitOrderedPairRecursion(recurse);

            Mark(replace);
            if (update is null)
            {
                EmitSeedReturn(tail, sharedLive);
            }
            else if (update is Expr.Binary { Operator: ErlangOperators.Plus } add)
            {
                owner.EmitTuplePart(head, 1, input);
                var previous = location;
                owner.EmitArithmeticLine(add.SourceLine, ref previous);
                owner.Op(GcBifTwo, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, sharedLive);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Plus, 2));
                owner.Operand(XRegisterTag, input);
                owner.Operand(XRegisterTag, seed.Register);
                owner.Operand(XRegisterTag, seed.Register);
                Heap(5, sharedLive);
                EmitResultPair(XRegisterTag, 0, seed.Register);
                EmitResultList(XRegisterTag, tail);
                owner.instructions.WriteByte(Return);
            }
            else
            {
                EmitOrderedPairEffectUpdate(function.Arity, update, seed.Singleton ? seed.Register : null);
            }

            Mark(nonCons);
            owner.EmitNilTest(XRegisterTag, input, functionInfo);
            EmitSeedReturn(-1, seed.Register + 1);

            return true;

            void Heap(int words, int live)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, words);
                owner.Operand(BeamWire.UnsignedTag, live);
            }

            void EmitSeedReturn(int suffix, int live)
            {
                Heap(5 + (seed.Singleton ? ConsHeapWords : 0), live);
                if (seed.Singleton)
                {
                    EmitSingleton(seed.Register);
                }

                EmitResultPair(XRegisterTag, 0, seed.Register);
                EmitResultList(XRegisterTag, suffix);
                owner.instructions.WriteByte(Return);
            }

            void EmitSingleton(int register)
            {
                owner.Op(PutList, XRegisterTag, register);
                owner.LiteralOperand(Nil.Value);
                owner.Operand(XRegisterTag, register);
            }

            void EmitResultPair(int keyTag, int keyRegister, int valueRegister)
            {
                owner.EmitTupleHeader(XRegisterTag, 2);
                owner.Operand(keyTag, keyRegister);
                owner.Operand(XRegisterTag, valueRegister);
            }

            void EmitResultList(int suffixTag, int suffix)
            {
                owner.Op(PutList, XRegisterTag, 0);
                if (suffix >= 0)
                {
                    owner.Operand(suffixTag, suffix);
                }
                else
                {
                    owner.LiteralOperand(Nil.Value);
                }

                owner.Operand(XRegisterTag, 0);
            }
        }

        private bool TryEmitOrderedPairRemovalOrUpdate(FunctionDefinition function, int functionInfo)
        {
            bool remove = function.Arity == 2 && function.Clauses.Count == 4;
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || !remove && (function.Arity != 3 || function.Clauses.Count != 2))
            {
                return false;
            }

            int input = function.Arity - 1;
            Clause next = function.Clauses[remove ? 1 : 0];
            Clause matched = function.Clauses[remove ? 2 : 1];
            if (ReadOrderedPairUpdateArm(next, input) is not { HeadAlias: { } pair } scan
                || ReadOrderedPairUpdateArm(matched, input) is not { } equal
                || !HasLookupComparison(
                    next.Guard,
                    ErlangOperators.Greater,
                    scan.Parameters[0],
                    scan.Index
                )
                || next.Body is not Expr.List
                {
                    Items: [Expr.Variable retained],
                    Tail: Expr.Call { Module: null, DynamicRemote: false } recur
                }
                || retained.Name != pair || recur.Function != function.Name || recur.Arguments.Count != function.Arity
                || !recur.Arguments.Take(input).Select((argument, index) =>
                    argument is Expr.Variable variable && variable.Name == scan.Parameters[index].Name).All(matches => matches)
                || recur.Arguments[^1] is not Expr.Variable nextTail || nextTail.Name != scan.Tail.Name
                || !entries.ContainsKey((recur.Function, recur.Arguments.Count)))
            {
                return false;
            }

            Expr.Apply? callback = null;
            if (remove)
            {
                Clause before = function.Clauses[0];
                Clause empty = function.Clauses[3];
                if (ReadOrderedPairUpdateArm(before, input) is not { } first
                    || !HasLookupComparison(
                        before.Guard,
                        ErlangOperators.Less,
                        first.Parameters[0],
                        first.Index
                    )
                    || !ReturnsOriginalPairList(before.Body, first)
                    || matched is not { Guard: null, Body: Expr.Variable dropped }
                    || dropped.Name != equal.Tail.Name
                    || empty is not { Guard: null, Patterns: [global::Erlang.Pattern.Variable, var nil] }
                    || ListEntryTest(nil) != IsNil
                    || !AttributeLiteral.TryNormalizeConstant(empty.Body, out Term closed) || closed is not Nil)
                {
                    return false;
                }
            }
            else if (!HasLookupComparison(
                matched.Guard,
                ErlangOperators.NumericEqual,
                equal.Parameters[0],
                equal.Index
            )
                || matched.Body is not Expr.List
                {
                    Items:
                    [
                        Expr.Tuple
                    {
                        Items:
                            [
                                Expr.Variable key,
                                Expr.Apply { Function: Expr.Variable functionValue, Arguments: [Expr.Variable payload] } invoke
                            ]
                    }
                    ],
                    Tail: Expr.Variable suffix
                }
                || key.Name != equal.Parameters[0].Name || suffix.Name != equal.Tail.Name
                || functionValue.Name != equal.Parameters[1].Name
                || payload.Name != equal.Value.Name || payload.Name == VariableScopeNames.Wildcard)
            {
                return false;
            }
            else
            {
                callback = invoke;
            }

            int descend = remove ? Label() : 0;
            int hit = Label();
            int nonCons = remove ? Label() : functionInfo;
            EmitOrderedPairUpdateEntry(function.Arity, nonCons, functionInfo);
            int indexRegister = function.Arity + 2;
            if (remove)
            {
                EmitOrderedPairLess(0, indexRegister, descend);
                owner.MoveToX0(XRegisterTag, input);
                owner.instructions.WriteByte(Return);
                Mark(descend);
            }

            EmitOrderedPairLess(indexRegister, 0, hit);
            EmitOrderedPairRecursion(recur);
            Mark(hit);
            if (remove)
            {
                owner.MoveToX0(XRegisterTag, function.Arity + 1);
                owner.instructions.WriteByte(Return);

                Mark(nonCons);
                owner.EmitNilTest(XRegisterTag, input, functionInfo);
                owner.EmitConstant(Nil.Value, 0);
                owner.instructions.WriteByte(Return);
            }
            else
            {
                // Partial update still rejects here, so do not reuse any input
                // register before the numeric equality test has succeeded.
                owner.Op(NumericEqualTest, LabelTag, functionInfo);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, indexRegister);
                EmitOrderedPairEffectUpdate(function.Arity, callback!);
            }

            return true;
        }

        private static bool ReturnsOriginalPairList(Expr expression, OrderedPairUpdateArm binding) =>
            expression is Expr.Variable original && original.Name == binding.ListAlias
            || expression is Expr.List { Items: [Expr.Variable head], Tail: Expr.Variable tail }
                && head.Name == binding.HeadAlias && tail.Name == binding.Tail.Name;

        private void EmitOrderedPairUpdateEntry(int arity, int nonCons, int functionInfo)
        {
            owner.EmitNonemptyList(arity - 1, nonCons);
            owner.EmitListParts(
                arity - 1,
                XRegisterTag,
                arity,
                XRegisterTag,
                arity + 1
            );
            owner.EmitTupleCheck(arity, 2, functionInfo);
            owner.EmitTuplePart(arity, 0, arity + 2);
        }

        private void EmitOrderedPairLess(int left, int right, int failure)
        {
            owner.Op(IsLess, LabelTag, failure);
            owner.Operand(XRegisterTag, left);
            owner.Operand(XRegisterTag, right);
        }

        private void EmitOrderedPairRecursion(Expr.Call recurse)
        {
            int arity = recurse.Arguments.Count;
            EmitFrameAllocation(arity + 2, 1);
            owner.MoveRegister(
                XRegisterTag,
                arity,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                arity + 1,
                XRegisterTag,
                arity - 1
            );
            owner.EmitCallLine(recurse, location);
            Call(null, recurse.Function, arity);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutList, YRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(1);
        }

        private void EmitOrderedPairEffectUpdate(int arity, Expr update, int? singletonSeed = null)
        {
            EmitFrameAllocation(arity + 2, 2, heapWords: singletonSeed.HasValue ? ConsHeapWords : 0);
            owner.MoveRegister(
                XRegisterTag,
                arity + 1,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );
            if (singletonSeed is int register)
            {
                owner.Op(PutList, XRegisterTag, register);
                owner.LiteralOperand(Nil.Value);
                owner.Operand(XRegisterTag, register);
            }

            owner.EmitTuplePart(arity, 1, 0);
            var previous = location;
            if (update is Expr.Apply apply)
            {
                owner.EmitArithmeticLine(apply.SourceLine, ref previous);
                owner.Op(CallFun, BeamWire.UnsignedTag, 1);
            }
            else
            {
                var append = (Expr.Binary)update;
                owner.EmitArithmeticLine(append.SourceLine, ref previous);
                Call(ErlangModuleNames.Module, append.Operator, 2);
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, 5);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.Operand(YRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(2);
        }

        private static OrderedPairUpdateArm? ReadOrderedPairUpdateArm(Clause clause, int input)
        {
            Pattern[] parameters = clause.Patterns.Where((_, index) => index != input).ToArray();
            if (parameters.Any(pattern => pattern is not global::Erlang.Pattern.Variable))
            {
                return null;
            }

            Pattern.Variable? listAlias = null;
            Pattern.Variable? headAlias = null;
            Pattern list = clause.Patterns[input];
            if (list is AliasPattern listWrapper)
            {
                if (!TryReadPatternAlias(listWrapper, out var binding, out list))
                {
                    return null;
                }

                listAlias = binding;
            }

            if (list is not Pattern.List { Items: [var head], Tail: Pattern.Variable tail })
            {
                return null;
            }

            if (head is AliasPattern headWrapper)
            {
                if (!TryReadPatternAlias(headWrapper, out var binding, out head))
                {
                    return null;
                }

                headAlias = binding;
            }

            if (head is not Pattern.Tuple { Items: [Pattern.Variable index, Pattern.Variable value] })
            {
                return null;
            }

            var inputs = parameters.Cast<Pattern.Variable>().ToArray();
            Pattern.Variable?[] bound = [.. inputs, listAlias, headAlias, index, value, tail];
            if (!DistinctBindings(bound.OfType<Pattern.Variable>()
                .Where(variable => variable.Name != VariableScopeNames.Wildcard).ToArray()))
            {
                return null;
            }

            return new(
                inputs,
                index,
                value,
                tail,
                listAlias?.Name,
                headAlias?.Name
            );
        }

        private static (int Register, bool Singleton)? ReadOrderedPairUpdateValue(Expr.Tuple pair, Pattern.Variable[] parameters) =>
            pair.Items is [Expr.Variable key, var value] && key.Name == parameters[0].Name
                ? ReadOrderedPairSeed(value, parameters) : null;

        private static (int Register, bool Singleton)? ReadOrderedPairSeed(Expr expression, Pattern.Variable[] parameters)
        {
            bool singleton = expression is Expr.List { Items: [Expr.Variable], Tail: null };
            var value = (singleton ? ((Expr.List)expression).Items[0] : expression) as Expr.Variable;
            int register = value is null ? -1 : Array.FindIndex(parameters, parameter => parameter.Name == value.Name);

            return register > 0 ? (register, singleton) : null;
        }
    }
}
