namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record InitialEntryProjection(
            string Name,
            int Input,
            int Field,
            string? Module,
            Expr.Call Call,
            Expr Continuation
        );

        private InitialEntryProjection? ReadInitialEntryProjection(
            Expr body,
            Dictionary<string, (int Input, int? Field)> bindings,
            Pattern.Variable[] parameters
        )
        {
            if (owner.ModuleOptimizationDisabled || body is not Expr.Sequence
                {
                    Items: [Expr.Call { Arguments: [Expr.Variable argument] } call, ..] items
                } || items.Count < 2 || !TryResolveOrdinaryCallTarget(call, out string? module)
                || !bindings.TryGetValue(argument.Name, out var source) || source.Field is not int field)
            {
                return null;
            }

            Expr continuation = items.Count == 2 ? items[1] : new Expr.Sequence(items.Skip(1).ToArray());
            if (ReadUsedEntryParameters(new Clause(parameters, null, continuation)) is not { } needed
                || needed.Contains(argument.Name))
            {
                return null;
            }

            return new InitialEntryProjection(
                argument.Name,
                source.Input,
                field,
                module,
                call,
                continuation
            );
        }
    }
}
