using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitDirectTryAfter(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [],
                Body: Expr.Try {
                    Clauses.Count: 0, Catches.Count: 0,
                    Body: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [] } body,
                    After: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [] } cleanup
                }
            }])
            {
                return false;
            }

            int handler = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.InitStackSlot(0);
            owner.Op(TryEnter, YRegisterTag, 1);
            owner.Operand(LabelTag, handler);
            owner.EmitCallLine(body, location);
            owner.EmitStaticRemoteCall(body);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 0);
            owner.Op(TryEnd, YRegisterTag, 1);
            var bodyLocation = CallLocation(body, location);
            owner.EmitCallLine(cleanup, bodyLocation);
            owner.EmitStaticRemoteCall(cleanup);
            Load(0);
            owner.EmitFrameReturn(2);

            Mark(handler);
            owner.Op(TryCase, YRegisterTag, 1);
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 0);
            owner.MoveRegister(XRegisterTag, 2, YRegisterTag, 1);
            owner.EmitLine(CallLocation(cleanup, bodyLocation));
            owner.EmitStaticRemoteCall(cleanup);
            owner.Op(Raise, YRegisterTag, 1);
            owner.Operand(YRegisterTag, 0);

            return true;
        }

        private void EmitTryAfter(Expr.Try expression, Expr cleanup)
        {
            var outerScope = new Dictionary<string, int>(variables, StringComparer.Ordinal);
            bool hasBranches = expression.Clauses.Count != 0 || expression.Catches.Count != 0;
            // The enclosed try owns a lower Y register than the cleanup handler.
            int? innerToken = hasBranches ? Slot() : null;
            int token = Slot();
            if (innerToken is int inner && inner > token)
            {
                innerToken = token;
                token = inner;
            }
            int handler = Label();
            int done = Label();
            owner.Op(TryEnter, YRegisterTag, token);
            owner.Operand(LabelTag, handler);
            if (!hasBranches)
            {
                Expression(expression.Body);
            }
            else
            {
                ProtectedExpression(expression, innerToken);
            }

            int result = Save();
            owner.Op(TryEnd, YRegisterTag, token);
            variables = new(outerScope, StringComparer.Ordinal);
            Discard(cleanup, null);
            Load(result);
            JumpTo(done);

            Mark(handler);
            owner.Op(TryCase, YRegisterTag, token);
            int reason = Slot();
            int trace = Slot();
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, reason);
            owner.MoveRegister(XRegisterTag, 2, YRegisterTag, trace);
            variables = new(outerScope, StringComparer.Ordinal);
            Discard(cleanup, null);
            Load(trace);
            Load(reason, 1);
            owner.Op(Raise, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);

            Mark(done);
            variables = outerScope;
        }
    }
}
