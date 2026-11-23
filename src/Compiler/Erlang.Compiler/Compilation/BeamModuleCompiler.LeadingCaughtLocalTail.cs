using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitLeadingCaughtLocalTail(FunctionDefinition function)
        {
            Clause clause = function.Clauses[0];
            if (owner.ModuleOptimizationDisabled || clause.Patterns is not [Pattern.Variable incoming]
                || ReadEntryTypeGuard(clause, clause.Guard) is not { Register: 0 } guard
                || clause.Body is not Expr.Case
                {
                    Value: Expr.Catch
                    {
                        Operand: Expr.Call { DynamicRemote: false, Arguments: [Expr.Variable protectedArgument] } call
                    },
                    Clauses:
                    [
                    {
                        Guard: null,
                        Patterns: [Pattern.Tuple
                        {
                            Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable { Name: VariableScopeNames.Wildcard }]
                        }],
                        Body: var failureValue
                    },
                    {
                        Guard: null,
                        Patterns: [Pattern.Variable caught],
                        Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable passed] } tail
                    }
                    ]
                }
                || incoming.Name != protectedArgument.Name || caught.Name != passed.Name || caught.Name == incoming.Name
                || caught.Name == VariableScopeNames.Wildcard
                || !AttributeLiteral.TryNormalizeConstant(failureValue, out Term error)
                || !TryResolveOrdinaryCall(call, out string? module)
                || !entries.TryGetValue((tail.Function, 1), out int target))
            {
                return false;
            }

            // The incoming value stays in X0 through catch setup. Both result
            // arms return, so the fallback entry guard needs no frame join.
            int handler = Label();
            int invoke = Label();
            int fallback = Label();
            owner.Op(guard.Opcode, LabelTag, fallback);
            owner.Operand(XRegisterTag, 0);
            EmitFrameAllocation(1, 1);
            EnterCatch(0, handler);
            owner.EmitCallLine(call, location);
            Call(module, call.Function, 1);
            EndCatch(0, handler);
            EmitDetailRecordTest(
                0,
                2,
                tag,
                invoke
            );
            owner.EmitConstant(error, 0);
            owner.EmitFrameReturn(1);
            Mark(invoke);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, target);
            owner.Operand(BeamWire.UnsignedTag, 1);
            Mark(fallback);

            return true;
        }
    }
}
