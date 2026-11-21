using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitEntryLiteralSend(Clause clause)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || clause is not
                {
                    Guard: null, Patterns: [Pattern.Variable receiver],
                    Body: Expr.Sequence
                    {
                        Items: [Expr.Binary { Operator: ErlangOperators.Send, Left: Expr.Variable destination } send, var result]
                    }
                } || receiver.Name == VariableScopeNames.Wildcard || receiver.Name != destination.Name
                || !AttributeLiteral.TryNormalizeConstant(send.Right, out Term message)
                || !AttributeLiteral.TryNormalizeConstant(result, out Term returnValue))
            {
                return false;
            }

            // X0 owns the receiver until Send. Its result is discarded, so a frame needs no Y slots.
            EmitFrameAllocation(1, 0);
            owner.EmitConstant(message, 1);
            var previous = location;
            owner.EmitArithmeticLine(send.SourceLine, ref previous);
            owner.instructions.WriteByte(Send);
            owner.EmitConstant(returnValue, 0);
            owner.EmitFrameReturn();

            return true;
        }
    }
}
