using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitPairedListAccumulator(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || function.Clauses is not [var first, var second])
            {
                return false;
            }

            bool startsWithList = first.Patterns is [Pattern.List { Items.Count: 2 }, _, _];
            Clause empty = startsWithList ? second : first;
            Clause loop = startsWithList ? first : second;
            if (empty is not
                {
                    Guard: null,
                    Patterns:
                    [
                        Pattern.Literal { Value: Nil } or Pattern.List { Items.Count: 0, Tail: null },
                        Pattern.Variable emptyLeft,
                        Pattern.Variable emptyRight
                    ],
                    Body: Expr.Tuple { Items: [Expr.Call reverseLeft, Expr.Call reverseRight] }
                }
                || !DistinctBindings(emptyLeft, emptyRight)
                || !IsReversal(reverseLeft, emptyLeft) || !IsReversal(reverseRight, emptyRight)
                || loop is not
                {
                    Guard: null,
                    Patterns:
                    [
                        Pattern.List { Items: [Pattern.Variable leftHead, Pattern.Variable rightHead], Tail: Pattern.Variable rest },
                        Pattern.Variable leftValues,
                        Pattern.Variable rightValues
                    ],
                    Body: Expr.Call
                    {
                        Module: null,
                        DynamicRemote: false,
                        Arguments: [Expr.Variable tail, Expr.List left, Expr.List right]
                    } recur
                }
                || !DistinctBindings(
                    leftHead,
                    rightHead,
                    rest,
                    leftValues,
                    rightValues
                )
                || recur.Function != function.Name || tail.Name != rest.Name
                || !Prepends(left, leftHead, leftValues) || !Prepends(right, rightHead, rightValues)
                || !entries.TryGetValue((function.Name, function.Arity), out int entry))
            {
                return false;
            }

            int fail = owner.nextLabel - 2;
            int finish = Label();
            const int pairArity = 2;
            const int inputCount = 3;
            const int frameSlots = 1;

            owner.EmitNonemptyList(0, finish);
            owner.EmitListParts(
                0,
                XRegisterTag,
                3,
                XRegisterTag,
                4
            );
            owner.EmitNonemptyList(4, fail);
            owner.Op(TestHeap, BeamWire.UnsignedTag, pairArity * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, inputCount + pairArity);
            owner.EmitListParts(
                4,
                XRegisterTag,
                0,
                XRegisterTag,
                4
            );
            Accumulate(3, 1);
            Accumulate(0, 2);
            owner.MoveToX0(XRegisterTag, 4);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, inputCount);
            owner.Operand(LabelTag, entry);

            Mark(finish);
            owner.EmitNilTest(XRegisterTag, 0, fail);
            EmitFrameAllocation(inputCount, frameSlots);
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                0
            );
            owner.MoveToX0(XRegisterTag, 1);
            owner.EmitCallLine(reverseLeft, location);
            Call(ListModuleNames.Module, ListModuleNames.Reverse, 1);
            owner.Op(Swap, YRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitCallLine(reverseRight, CallLocation(reverseLeft, location));
            Call(ListModuleNames.Module, ListModuleNames.Reverse, 1);
            owner.Op(TestHeap, BeamWire.UnsignedTag, pairArity + 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(0, (YRegisterTag, 0), (XRegisterTag, 0));
            owner.EmitFrameReturn(frameSlots);

            return true;

            bool IsReversal(Expr.Call call, Pattern.Variable argument) =>
                call.Function == ListModuleNames.Reverse
                && call.Arguments is [Expr.Variable input] && input.Name == argument.Name
                && TryResolveOrdinaryCallTarget(call, out string? module) && module == ListModuleNames.Module;

            static bool Prepends(Expr.List candidate, Pattern.Variable element, Pattern.Variable accumulator) =>
                candidate is { Items: [Expr.Variable head], Tail: Expr.Variable tail }
                && head.Name == element.Name && tail.Name == accumulator.Name;

            void Accumulate(int head, int accumulator)
            {
                owner.Op(PutList, XRegisterTag, head);
                owner.Operand(XRegisterTag, accumulator);
                owner.Operand(XRegisterTag, accumulator);
            }
        }
    }
}
