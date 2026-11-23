using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitForkedCallList(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Body: Expr.Sequence
                    {
                        Items:
                        [
                            Expr.Match { Pattern: Pattern.Variable input, Value: Expr.Call producer },
                            Expr.Match { Pattern: Pattern.Variable first, Value: Expr.Call firstBranch },
                            Expr.Match { Pattern: Pattern.Variable second, Value: Expr.Call secondBranch },
                            Expr.List { Items: [Expr.Call finishSecond, Expr.Call finishFirst], Tail: null }
                        ]
                    }
                } clause]
                || !HasUnconditionalParameters(clause) || !DistinctBindings(input, first, second))
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Contains(input.Name) || parameters.Contains(first.Name) || parameters.Contains(second.Name)
                || ReadEntryCall(producer, parameters) is not { } start
                || ReadUnary(firstBranch, input.Name) is not { } branchOne
                || ReadUnary(secondBranch, input.Name) is not { } branchTwo
                || ReadUnary(finishSecond, second.Name) is not { } finishTwo
                || ReadUnary(finishFirst, first.Name) is not { } finishOne)
            {
                return false;
            }

            const int retainedSlot = 0;
            const int frame = 1;
            EmitFrameAllocation(EntryCallLiveCount(start, parameters), frame);
            EmitFrameInitialization(retainedSlot, frame);
            EmitEntryCall(start);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                retainedSlot
            );
            BeamSourceLocation previous = CallLocation(start.Call, location);
            EmitNextCall(branchOne);
            // The producer's last read and the first branch's spill share one slot.
            Exchange();
            EmitNextCall(branchTwo);
            EmitNextCall(finishTwo);
            Exchange();
            EmitNextCall(finishOne);

            owner.Op(TestHeap, BeamWire.UnsignedTag, 2 * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 0);
            owner.Op(PutList, YRegisterTag, retainedSlot);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(frame);

            return true;

            EntryCall? ReadUnary(Expr.Call call, string argument) =>
                call.Arguments is [Expr.Variable read] && read.Name == argument ? ReadEntryCall(call, [argument]) : null;

            void EmitNextCall(EntryCall call)
            {
                EmitEntryCall(call, previous);
                previous = CallLocation(call.Call, previous);
            }

            void Exchange()
            {
                owner.Op(Swap, YRegisterTag, retainedSlot);
                owner.Operand(XRegisterTag, 0);
            }
        }
    }
}
