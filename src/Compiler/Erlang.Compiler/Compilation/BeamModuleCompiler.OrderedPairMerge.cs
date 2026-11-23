using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record MergePairBinding(
            Pattern.Variable Key,
            Pattern.Variable Value,
            Pattern.Variable Tail,
            Pattern.Variable? HeadAlias,
            Pattern.Variable? ListAlias
        );

        private bool TryEmitOrderedPairMerge(FunctionDefinition function, int functionInfo)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Arity != 3 || function.Clauses is not [var less, var more, { Guard: null } equal, var leftEmpty, var rightEmpty]
                || ReadOrderedPairMergeArm(less) is not { } first
                || ReadOrderedPairMergeArm(more) is not { } second
                || ReadOrderedPairMergeArm(equal) is not { } both
                || !HasLookupComparison(
                    less.Guard,
                    ErlangOperators.Less,
                    first.Left.Key,
                    first.Right.Key
                )
                || !HasLookupComparison(
                    more.Guard,
                    ErlangOperators.Greater,
                    second.Left.Key,
                    second.Right.Key
                )
                || !IsPairMergeEmptyArm(leftEmpty, 1) || !IsPairMergeEmptyArm(rightEmpty, 2)
                || less.Body is not Expr.List { Items: [Expr.Variable prependLeft], Tail: Expr.Call recurLeft }
                || !IsOrderedPairMergeRecursion(recurLeft, function.Name, first.Function.Name)
                || prependLeft.Name != first.Left.HeadAlias?.Name
                || recurLeft.Arguments[1] is not Expr.Variable nextLeft || nextLeft.Name != first.Left.Tail.Name
                || !ReturnsMergeList(recurLeft.Arguments[2], first.Right)
                || more.Body is not Expr.List { Items: [Expr.Variable prependRight], Tail: Expr.Call recurRight }
                || !IsOrderedPairMergeRecursion(recurRight, function.Name, second.Function.Name)
                || prependRight.Name != second.Right.HeadAlias?.Name
                || recurRight.Arguments[2] is not Expr.Variable nextRight || nextRight.Name != second.Right.Tail.Name
                || !ReturnsMergeList(recurRight.Arguments[1], second.Left)
                || equal.Body is not Expr.List
                {
                    Items:
                    [
                        Expr.Tuple
                    {
                        Items:
                            [
                                Expr.Variable retainedKey,
                                Expr.Apply
                            {
                                Function: Expr.Variable callback,
                                Arguments: [Expr.Variable key, Expr.Variable leftValue, Expr.Variable rightValue]
                            } apply
                            ]
                    }
                    ],
                    Tail: Expr.Call recurBoth
                }
                || callback.Name != both.Function.Name || retainedKey.Name != both.Left.Key.Name || key.Name != retainedKey.Name
                || leftValue.Name != both.Left.Value.Name || rightValue.Name != both.Right.Value.Name
                || !IsOrderedPairMergeRecursion(recurBoth, function.Name, both.Function.Name)
                || recurBoth.Arguments[1] is not Expr.Variable leftTail || leftTail.Name != both.Left.Tail.Name
                || recurBoth.Arguments[2] is not Expr.Variable rightTail || rightTail.Name != both.Right.Tail.Name
                || !entries.ContainsKey((function.Name, 3)))
            {
                return false;
            }

            int compareRight = Label();
            int sameKey = Label();
            int leftNil = Label();
            int rightNil = Label();

            // Neither input list is overwritten while a guarded nil arm or an
            // argument-preserving function_clause rejection is still possible.
            owner.EmitNonemptyList(1, leftNil);
            owner.EmitListParts(
                1,
                XRegisterTag,
                3,
                XRegisterTag,
                4
            );
            owner.EmitTupleCheck(3, 2, rightNil);
            owner.EmitNonemptyList(2, rightNil);
            owner.EmitListParts(
                2,
                XRegisterTag,
                5,
                XRegisterTag,
                6
            );
            owner.EmitTupleCheck(5, 2, rightNil);
            owner.EmitTuplePart(3, 0, 7);
            owner.EmitTuplePart(5, 0, 8);
            EmitOrderedPairLess(7, 8, compareRight);
            EmitOrderedPairMergePrepend(
                recurLeft,
                input: 1,
                head: 3,
                tail: 4
            );

            Mark(compareRight);
            EmitOrderedPairLess(8, 7, sameKey);
            EmitOrderedPairMergePrepend(
                recurRight,
                input: 2,
                head: 5,
                tail: 6
            );

            Mark(sameKey);
            EmitFrameAllocation(8, 4);
            owner.MoveRegister(
                XRegisterTag,
                6,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                7,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                4,
                YRegisterTag,
                2
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                3
            );
            owner.EmitTuplePart(5, 1, 2);
            owner.EmitTuplePart(3, 1, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                3
            );
            owner.MoveToX0(XRegisterTag, 7);
            var previous = location;
            owner.EmitArithmeticLine(apply.SourceLine, ref previous);
            owner.Op(CallFun, BeamWire.UnsignedTag, 3);
            Load(2, 1);
            Load(3, 3);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                3
            );
            Load(0, 2);
            owner.MoveRegister(
                YRegisterTag,
                1,
                YRegisterTag,
                2
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveToX0(XRegisterTag, 3);
            owner.EmitCallLine(recurBoth, previous);
            Call(null, recurBoth.Function, 3);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 5);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 2, 1);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(YRegisterTag, 1);
            owner.Op(PutList, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(2);

            Mark(leftNil);
            EmitOrderedPairMergeEmpty(1, rightNil);
            Mark(rightNil);
            EmitOrderedPairMergeEmpty(2, functionInfo);

            return true;
        }

        private void EmitOrderedPairMergePrepend(
            Expr.Call recurse,
            int input,
            int head,
            int tail
        )
        {
            EmitFrameAllocation(tail + 1, 1);
            owner.MoveRegister(
                XRegisterTag,
                head,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                tail,
                XRegisterTag,
                input
            );
            owner.EmitCallLine(recurse, location);
            Call(null, recurse.Function, 3);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutList, YRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(1);
        }

        private void EmitOrderedPairMergeEmpty(int input, int failure)
        {
            owner.EmitNilTest(XRegisterTag, input, failure);
            owner.Op(IsFunctionWithArity, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(Term.I(3));
            owner.MoveToX0(XRegisterTag, 3 - input);
            owner.instructions.WriteByte(Return);
        }

        private bool IsPairMergeEmptyArm(Clause clause, int tested)
        {
            return ListEntryTest(clause.Patterns[tested]) == IsNil
                && clause.Patterns[0] is Pattern.Variable function
                && clause.Patterns[3 - tested] is Pattern.Variable rest
                && DistinctBindings(function, rest)
                && clause.Body is Expr.Variable returned && returned.Name == rest.Name
                && clause.Guard is Expr.Call
                {
                    Module: null or ErlangModuleNames.Module,
                    DynamicRemote: false,
                    Function: AutoImportNames.IsFunction,
                    Arguments: [Expr.Variable checkedFun, Expr.Literal { Value: Integer arity }]
                } guard
                && checkedFun.Name == function.Name && arity.Value == 3 && owner.ResolvesErlangBif(guard);
        }

        private static (Pattern.Variable Function, MergePairBinding Left, MergePairBinding Right)? ReadOrderedPairMergeArm(Clause clause)
        {
            if (clause.Patterns is not [Pattern.Variable function, var left, var right]
                || ReadMergePairBinding(left) is not { } first || ReadMergePairBinding(right) is not { } second)
            {
                return null;
            }

            Pattern.Variable?[] bindings = [
                function, first.Key, first.Value, first.Tail, first.HeadAlias, first.ListAlias,
                second.Key, second.Value, second.Tail, second.HeadAlias, second.ListAlias
            ];
            if (function.Name == VariableScopeNames.Wildcard || !DistinctBindings(bindings.OfType<Pattern.Variable>()
                .Where(binding => binding.Name != VariableScopeNames.Wildcard).ToArray()))
            {
                return null;
            }

            return (function, first, second);
        }

        private static MergePairBinding? ReadMergePairBinding(Pattern pattern)
        {
            Pattern.Variable? listAlias = null;
            if (pattern is AliasPattern list)
            {
                if (!TryReadPatternAlias(list, out var bound, out pattern))
                {
                    return null;
                }

                listAlias = bound;
            }

            if (pattern is not Pattern.List { Items: [var head], Tail: Pattern.Variable tail })
            {
                return null;
            }

            Pattern.Variable? headAlias = null;
            if (head is AliasPattern pair)
            {
                if (!TryReadPatternAlias(pair, out var bound, out head))
                {
                    return null;
                }

                headAlias = bound;
            }

            return head is Pattern.Tuple { Items: [Pattern.Variable key, Pattern.Variable value] }
                ? new(
                    key,
                    value,
                    tail,
                    headAlias,
                    listAlias
                ) : null;
        }

        private static bool IsOrderedPairMergeRecursion(Expr.Call call, string name, string function) =>
            call is { Module: null, DynamicRemote: false, Arguments: [Expr.Variable input, _, _] }
            && call.Function == name && input.Name == function;

        private static bool ReturnsMergeList(Expr candidate, MergePairBinding binding) =>
            candidate is Expr.Variable original && original.Name == binding.ListAlias?.Name
            || candidate is Expr.List { Items: [Expr.Variable head], Tail: Expr.Variable tail }
                && head.Name == binding.HeadAlias?.Name && tail.Name == binding.Tail.Name;
    }
}
