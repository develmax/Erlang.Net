using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSharedConsInsertion(FunctionDefinition function, int functionInfo)
        {
            if (function.Arity != 2 || function.Clauses is not [var first, var second, { Guard: null } third, { Guard: null } empty])
            {
                return false;
            }

            const int head = 2;
            const int tail = 3;
            int input = ListEntryTest(empty.Patterns[0]) == IsNil ? 0
                : ListEntryTest(empty.Patterns[1]) == IsNil ? 1 : -1;
            if (input < 0 || empty.Patterns[1 - input] is not Pattern.Variable emptyKey
                || emptyKey.Name == VariableScopeNames.Wildcard)
            {
                return false;
            }

            int key = 1 - input;
            string[] emptyInputs = [VariableScopeNames.Wildcard, VariableScopeNames.Wildcard];
            emptyInputs[key] = emptyKey.Name;
            if (ReadBindings(first) is not { } firstInputs
                || ReadBindings(second) is not { } secondInputs
                || ReadBindings(third) is not { } thirdInputs
                || !IsOrdered(
                    first.Guard,
                    firstInputs,
                    head,
                    key
                )
                || !IsOrdered(
                    second.Guard,
                    secondInputs,
                    key,
                    head
                )
                || ReadConsCallResult(first.Body, firstInputs) is not { HeadCall: null, Tuple: null, Saved: [head] } recursion
                || EntryCallLiveCount(recursion.Call, firstInputs) != tail + 1
                || ReadEntryConstructedArgument(second.Body, 0, secondInputs) is not
                { IsTuple: false, Operands: [{ Source: int inserted }, { Source: int suffix }] } prepend
                || inserted != key || suffix != input
                || third.Body is not Expr.Variable returned || returned.Name == VariableScopeNames.Wildcard
                || thirdInputs[input] != returned.Name
                || ReadEntryConstructedArgument(empty.Body, 0, emptyInputs) is not
                { IsTuple: false, Operands: [{ Source: int singletonHead }, { Constant: Nil }] } singleton
                || singletonHead != key)
            {
                return false;
            }

            int considerPrepend = Label();
            int unchanged = Label();
            int nonCons = Label();
            owner.EmitNonemptyList(input, nonCons);
            owner.EmitListParts(
                input,
                XRegisterTag,
                head,
                XRegisterTag,
                tail
            );
            EmitLess(head, key, considerPrepend);
            EmitConsCallResult(recursion, tail + 1);
            Mark(considerPrepend);
            EmitLess(key, head, unchanged);
            EmitConstructedReturn(prepend, function.Arity);
            Mark(unchanged);
            if (input != 0)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    input,
                    XRegisterTag,
                    0
                );
            }

            owner.instructions.WriteByte(Return);
            Mark(nonCons);
            owner.EmitNilTest(XRegisterTag, input, functionInfo);
            EmitConstructedReturn(singleton, key + 1);

            return true;

            string[]? ReadBindings(Clause clause)
            {
                if (clause.Patterns[key] is not Pattern.Variable keyBinding)
                {
                    return null;
                }

                Pattern pattern = clause.Patterns[input];
                string[] bindings = Enumerable.Repeat(VariableScopeNames.Wildcard, tail + 1).ToArray();
                if (pattern is AliasPattern alias)
                {
                    if (!TryReadPatternAlias(alias, out var binding, out pattern))
                    {
                        return null;
                    }

                    bindings[input] = binding.Name;
                }

                if (pattern is not Pattern.List { Items: [Pattern.Variable value], Tail: Pattern.Variable rest })
                {
                    return null;
                }

                bindings[key] = keyBinding.Name;
                bindings[head] = value.Name;
                bindings[tail] = rest.Name;
                string[] names = bindings.Where(name => name != VariableScopeNames.Wildcard).ToArray();

                return names.Length == names.Distinct(StringComparer.Ordinal).Count() ? bindings : null;
            }

            void EmitLess(int left, int right, int failure)
            {
                owner.Op(IsLess, LabelTag, failure);
                owner.Operand(XRegisterTag, left);
                owner.Operand(XRegisterTag, right);
            }

            void EmitConstructedReturn(EntryConstructedArgument result, int live)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, result.HeapWords);
                owner.Operand(BeamWire.UnsignedTag, live);
                EmitEntryConstruction(result);
                owner.instructions.WriteByte(Return);
            }

            static bool IsOrdered(
                Expr? guard,
                string[] bindings,
                int left,
                int right
            )
            {
                if (guard is not Expr.Binary
                    {
                        Operator: ErlangOperators.Less or ErlangOperators.Greater,
                        Left: Expr.Variable firstOperand, Right: Expr.Variable secondOperand
                    } comparison
                    || bindings[left] == VariableScopeNames.Wildcard || bindings[right] == VariableScopeNames.Wildcard)
                {
                    return false;
                }

                if (comparison.Operator == ErlangOperators.Greater)
                {
                    (left, right) = (right, left);
                }

                return firstOperand.Name == bindings[left] && secondOperand.Name == bindings[right];
            }
        }
    }
}
