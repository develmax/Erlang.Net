using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitEntryRegisterSend(Clause clause)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || clause is not { Guard: null, Patterns: [Pattern.Variable receiver, ..] }
                || receiver.Name == VariableScopeNames.Wildcard || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            Expr operation = clause.Body;
            Term? returnValue = null;
            if (operation is Expr.Sequence { Items: [var first, var continuation] })
            {
                if (!AttributeLiteral.TryNormalizeConstant(continuation, out Term constant))
                {
                    return false;
                }

                operation = first;
                returnValue = constant;
            }

            if (operation is not Expr.Binary { Operator: ErlangOperators.Send, Left: Expr.Variable destination } send
                || receiver.Name != destination.Name)
            {
                return false;
            }

            var inputs = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < clause.Patterns.Count; index++)
            {
                var input = (Pattern.Variable)clause.Patterns[index];
                if (input.Name != VariableScopeNames.Wildcard)
                {
                    inputs.Add(input.Name, index);
                }
            }

            const int messageRegister = 1;
            if (AttributeLiteral.TryNormalizeConstant(send.Right, out Term message))
            {
                EmitFrameAllocation(1, 0);
                owner.EmitConstant(message, messageRegister);
            }
            else if (send.Right is Expr.Variable payload && inputs.TryGetValue(payload.Name, out int source))
            {
                EmitFrameAllocation(source + 1, 0);
                owner.MoveRegister(
                    XRegisterTag,
                    source,
                    XRegisterTag,
                    messageRegister
                );
            }
            else if (ReadCallArgumentConstructions(
                [destination, send.Right],
                result: -1,
                saved: new Dictionary<string, int>(),
                inputs: inputs,
                preserveX0: true
            ) is { } plan)
            {
                EmitFrameAllocation(plan.Live, 0, plan.Constructions.Sum(construction => construction.HeapWords));
                EmitCallArgumentConstructions(plan, -1, heapChecked: true);
            }
            else
            {
                return false;
            }

            // Send consumes the original recipient and returns the message in X0.
            var previous = location;
            owner.EmitArithmeticLine(send.SourceLine, ref previous);
            owner.instructions.WriteByte(Send);
            if (returnValue is not null)
            {
                owner.EmitConstant(returnValue, 0);
            }

            owner.EmitFrameReturn();

            return true;
        }
    }
}
