using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitListGuardedTupleApplyEntry(FunctionDefinition function, int functionInfo)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || function.Arity != 3
                || function.Clauses is not [
                { Guard: Expr.Call listGuard, Body: Expr.Call listTail } list,
                { Guard: Expr.Call pairGuard, Body: Expr.Call pairTail } pair,
                { Guard: null, Body: Expr.Call taggedTail } tagged
                ] || list.Patterns is not [Pattern.Variable listFirst, Pattern.Variable listSecond, Pattern.Variable listValue]
                || pair.Patterns is not [
                    Pattern.Variable pairFirst, Pattern.Variable pairSecond,
                    Pattern.Tuple { Items: [Pattern.Variable pairValue, Pattern.Variable pairRest] }
                ] || tagged.Patterns is not [
                    Pattern.Variable taggedFirst, Pattern.Variable taggedSecond,
                    Pattern.Tuple
                {
                    Items: [
                            Pattern.Literal { Value: Atom tag }, Pattern.Variable module, Pattern.Variable argument
                        ]
                }
                ] || !DistinctBindings(listFirst, listSecond, listValue)
                || !DistinctBindings(
                    pairFirst,
                    pairSecond,
                    pairValue,
                    pairRest
                ) || !DistinctBindings(
                    taggedFirst,
                    taggedSecond,
                    module,
                    argument
                )
                || !IsUnshadowedListGuard(listGuard, listValue) || !IsUnshadowedListGuard(pairGuard, pairValue)
                || !ReadsTail(
                    listTail,
                    listTail.Function,
                    listFirst,
                    listSecond,
                    out var listInput,
                    out var listExtra
                )
                || !ReadsTail(
                    pairTail,
                    listTail.Function,
                    pairFirst,
                    pairSecond,
                    out var pairInput,
                    out var pairExtra
                )
                || !ReadsTail(
                    taggedTail,
                    listTail.Function,
                    taggedFirst,
                    taggedSecond,
                    out var taggedInput,
                    out var taggedExtra
                )
                || listInput is not Expr.Variable firstList || firstList.Name != listValue.Name
                || pairInput is not Expr.Variable pairList || pairList.Name != pairValue.Name
                || pairExtra is not Expr.Variable pairData || pairData.Name != pairRest.Name
                || !IsEmptyList(listExtra) || !IsEmptyList(taggedExtra)
                || taggedInput is not Expr.Call invocation
                || ReadFixedApply(invocation) is not
                {
                    Module: Expr.Variable moduleOperand,
                    Function: Expr.Literal { Value: Atom member },
                    Arguments: [Expr.Variable argumentOperand]
                } || moduleOperand.Name != module.Name || argumentOperand.Name != argument.Name
                || ReadEntryCall(listTail, [listFirst.Name, listSecond.Name, listValue.Name]) is not { Module: null } tail)
            {
                return false;
            }

            const int selector = 2;
            const int scratch = 3;
            const int frameSlots = 2;
            int tupleEntry = Label();
            int taggedEntry = Label();
            int pairEntry = Label();

            EmitListGuard(selector, tupleEntry);
            owner.EmitOrderedTailMoves(tail.Moves, tail.Constants);
            EmitRegisterTailTarget(listTail, listTail.Arguments.Count);

            Mark(tupleEntry);
            owner.EmitTupleArityDispatch(functionInfo, [(2, pairEntry), (3, taggedEntry)], selector);

            // Failures before the call retain the original three MFA arguments.
            Mark(taggedEntry);
            owner.EmitTuplePart(selector, 0, scratch);
            owner.Op(ExactEqualTest, LabelTag, functionInfo);
            owner.Operand(XRegisterTag, scratch);
            owner.LiteralOperand(tag);
            EmitFrameAllocation(function.Arity, frameSlots);
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
            owner.EmitTuplePart(selector, 1, 0);
            owner.EmitTuplePart(selector, 2, 1);
            owner.EmitConstant(member, selector);
            EmitSwap(0, 1);
            owner.EmitCallLine(invocation, location);
            owner.Op(DynamicApply, BeamWire.UnsignedTag, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                selector
            );
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.EmitConstant(Nil.Value, scratch);
            owner.MoveRegister(
                YRegisterTag,
                1,
                XRegisterTag,
                0
            );
            EmitPreparedFrameTail(tail, CallLocation(invocation, location), frameSlots);

            Mark(pairEntry);
            owner.EmitTuplePart(selector, 0, scratch);
            EmitListGuard(scratch, functionInfo);
            owner.EmitTuplePart(selector, 1, selector);
            EmitSwap(selector, scratch);
            EmitRegisterTailTarget(pairTail, pairTail.Arguments.Count);

            return true;

            bool IsUnshadowedListGuard(Expr.Call guard, Pattern.Variable input) =>
                IsListGuard(guard, input.Name)
                && (guard.Module is not null || owner.ResolveExternalModule(guard.Function, 1) == ErlangModuleNames.Module);

            static bool ReadsTail(
                Expr.Call candidate,
                string function,
                Pattern.Variable first,
                Pattern.Variable second,
                out Expr input,
                out Expr extra
            )
            {
                if (!candidate.DynamicRemote && candidate.Module is null && candidate.Function == function
                    && candidate.Arguments is [Expr.Variable a, Expr.Variable b, var value, var data]
                    && a.Name == first.Name && b.Name == second.Name)
                {
                    input = value;
                    extra = data;

                    return true;
                }

                input = null!;
                extra = null!;

                return false;
            }

            static bool IsEmptyList(Expr expression) =>
                AttributeLiteral.TryNormalizeConstant(expression, out Term constant) && constant is Nil;

            void EmitListGuard(int input, int fail)
            {
                owner.Op(IsList, LabelTag, fail);
                owner.Operand(XRegisterTag, input);
            }

            void EmitSwap(int first, int second)
            {
                owner.Op(Swap, XRegisterTag, first);
                owner.Operand(XRegisterTag, second);
            }
        }
    }
}
