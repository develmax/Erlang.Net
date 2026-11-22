using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitForwardReceivedMessageLoop(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || !entries.TryGetValue((function.Name, function.Arity), out int entry)
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [Pattern.Variable recipient],
                        Body: Expr.Receive
                        {
                            Timeout: null, After: null, Clauses:
                            [
                                {
                                    Guard: null,
                                    Patterns: [Pattern.Variable message],
                                    Body: Expr.Sequence
                                    {
                                        Items:
                                        [
                                            Expr.Binary
                                        {
                                            Operator: ErlangOperators.Send,
                                            Left: Expr.Variable destination,
                                            Right: Expr.Variable payload
                                        } send,
                                            Expr.Call
                                        {
                                            Module: null, DynamicRemote: false,
                                            Arguments: [Expr.Variable retained]
                                        } tail
                                        ]
                                    }
                                }
                            ]
                        }
                    }
                ]
                || !DistinctBindings(recipient, message)
                || destination.Name != recipient.Name || payload.Name != message.Name
                || retained.Name != recipient.Name || tail.Function != function.Name)
            {
                return false;
            }

            int scan = Label();
            int wait = Label();
            EmitFrameAllocation(1, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            Mark(scan);
            owner.Op(LoopReceive, LabelTag, wait);
            owner.Operand(XRegisterTag, 0);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.instructions.WriteByte(RemoveMessage);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                0
            );
            var previous = location;
            owner.EmitArithmeticLine(send.SourceLine, ref previous);
            owner.instructions.WriteByte(Send);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                0
            );
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, entry);
            owner.Operand(BeamWire.UnsignedTag, 1);
            Mark(wait);
            owner.Op(Wait, LabelTag, scan);

            return true;
        }
    }
}
