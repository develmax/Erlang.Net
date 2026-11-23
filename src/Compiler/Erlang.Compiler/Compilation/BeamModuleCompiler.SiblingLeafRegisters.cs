using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSiblingLeafRegisters(Clause clause)
        {
            if (clause.Guard is not null || clause.Patterns.Count < 2 || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Tuple tuple || !tuple.Items.Any(item => item is Expr.Variable))
            {
                return false;
            }

            if (tuple.Items.Count(field => field is Expr.Tuple or Expr.List
                    && !AttributeLiteral.TryNormalizeConstant(field, out _)) < 2)
            {
                return false;
            }

            var inputs = clause.Patterns.Cast<Pattern.Variable>()
                .Select((parameter, register) => (parameter.Name, Register: register))
                .Where(input => input.Name != VariableScopeNames.Wildcard)
                .ToDictionary(input => input.Name, input => input.Register, StringComparer.Ordinal);
            if (ReadCallArgumentConstructions(
                [tuple],
                -1,
                new Dictionary<string, int>(),
                inputs: inputs
            ) is not { Loads.Count: 0, Moves.Count: 0, Constants.Count: 0 } plan
                || !plan.Constructions.SkipLast(1).Any(construction => construction.Operands.Any(operand =>
                    operand.Tag == XRegisterTag && operand.Register == construction.Register)))
            {
                return false;
            }

            EmitCallArgumentConstructions(plan, -1);
            owner.instructions.WriteByte(Return);

            return true;
        }
    }
}
