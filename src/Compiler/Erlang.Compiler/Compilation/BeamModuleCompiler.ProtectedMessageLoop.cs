using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitProtectedMessageLoop(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || function.Arity != 0 || !entries.TryGetValue((function.Name, 0), out int entry)
                || function.Clauses is not
                [
                    { Guard: null, Body: Expr.Sequence { Items: [var first, var last] } }
                ]
                || UnwrapWildcardMatch(last) is not Expr.Call { Module: null, DynamicRemote: false, Arguments: [] } retry
                || retry.Function != function.Name
                || UnwrapWildcardMatch(first) is not Expr.Receive
                {
                    Timeout: null,
                    After: null,
                    Clauses:
                    [
                        {
                            Guard: null,
                            Patterns: [Pattern.Variable received],
                            Body: Expr.Try
                            {
                                Clauses: [],
                                After: null,
                                Body: Expr.Call { Arguments: [Expr.Variable argument] } call,
                                Catches:
                                [
                                {
                                    Guard: null,
                                    Patterns: [Pattern.Tuple { Items: var caught }],
                                    Body: Expr.Literal
                                }
                                ]
                            }
                        }
                    ]
                }
                || received.Name == VariableScopeNames.Wildcard || received.Name != argument.Name
                || caught.Count != 3 || caught.Any(item =>
                    item is not global::Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard })
                || !TryResolveOrdinaryCallTarget(call, out string? module))
            {
                return false;
            }

            int scan = Label();
            int handler = Label();
            int resume = Label();
            int wait = Label();
            const int handlerFrame = 1;
            const int tryToken = 0;

            // The only live value after remove_message is the outgoing message.
            // The matching protected region owns one Y try tag. Both results
            // are ignored before the tail call resumes the receive loop.
            EmitFrameAllocation(arity: 0, size: handlerFrame);
            EmitFrameInitialization(0, handlerFrame);
            Mark(scan);
            owner.Op(LoopReceive, LabelTag, wait);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(RemoveMessage);
            owner.Op(TryEnter, YRegisterTag, tryToken);
            owner.Operand(LabelTag, handler);
            owner.EmitCallLine(call, location);
            Call(module, call.Function, call.Arguments.Count);
            owner.Op(TryEnd, YRegisterTag, tryToken);
            JumpTo(resume);
            Mark(handler);
            owner.Op(TryCase, YRegisterTag, tryToken);
            Mark(resume);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, retry.Arguments.Count);
            owner.Operand(LabelTag, entry);
            owner.Operand(BeamWire.UnsignedTag, handlerFrame);
            Mark(wait);
            owner.Op(Wait, LabelTag, scan);

            return true;
        }
    }
}
