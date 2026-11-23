using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitFramelessElementArgument(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || function.Clauses is not
                [
                    {
                        Guard: null, Patterns: [Pattern.Variable input],
                        Body: Expr.Sequence { Items: [var ignored, Expr.Literal { Value: Atom result }] }
                    }
                ]
                || !DistinctBindings(input) || owner.IsDeclaredNative((function.Name, 1))
                || ReadDiscardedCall(ignored) is not
                {
                    Module: null, DynamicRemote: false,
                    Arguments:
                    [
                        var argument,
                        Expr.Call { Arguments: [Expr.Literal { Value: Integer position }, Expr.Variable source] } element
                    ]
                } effect
                || source.Name != input.Name || !IsElementGuardCall(element) || !owner.ResolvesErlangBif(element)
                || !AttributeLiteral.TryNormalizeConstant(argument, out Term literal)
                || !entries.TryGetValue((effect.Function, 2), out int entry) || owner.IsDeclaredNative((effect.Function, 2)))
            {
                return false;
            }

            // The entry register is still the tuple operand. The potentially
            // failing BIF needs no call frame and the following call keeps no Y values.
            owner.EmitCallLine(element, location);
            EmitElementInstruction(failure: 0);
            owner.LiteralOperand(position);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);

            EmitFrameAllocation(1, 0);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.EmitConstant(literal, 0);
            owner.EmitCallLine(effect, CallLocation(element, location));
            owner.EmitFieldCall(2, entry);
            owner.EmitConstant(result, 0);
            owner.EmitFrameReturn(0);

            return true;

            static Expr.Call? ReadDiscardedCall(Expr expression) => expression switch
            {
                Expr.Call call => call,
                Expr.Match { Pattern: Pattern.Variable { Name: VariableScopeNames.Wildcard }, Value: Expr.Call call } => call,
                _ => null
            };
        }
    }
}
