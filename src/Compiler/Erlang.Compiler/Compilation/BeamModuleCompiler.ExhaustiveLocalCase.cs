namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private Expr.Case RewriteExhaustiveLocalCase(Expr.Case decision)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || owner.OnLoadFunction is not null || owner.SourceModule?.DeclaredNifs is { Count: > 0 }
                || decision.Value is not Expr.Call { Module: null, DynamicRemote: false } selector
                || owner.ResolvesErlangBif(selector) || owner.receiveScratchRegisters is not null
                || decision.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable field] }]
                } tagged,
                { Guard: null, Patterns: [Pattern.Literal { Value: Atom atom }] } otherwise
                ]
                || field.Name != VariableScopeNames.Wildcard
                    && (variables.ContainsKey(field.Name) || deferredPairMembers.ContainsKey(field.Name))
                || !localFunctions.TryGetValue((selector.Function, selector.Arguments.Count), out var function)
                || !function.Clauses.All(clause => EndsWithCoveredResult(clause.Body)))
            {
                return decision;
            }

            // The local call can return only the tagged pair or the fallback atom.
            return decision with
            {
                Clauses = [tagged, otherwise with { Patterns = [new Pattern.Variable(VariableScopeNames.Wildcard)] }]
            };

            bool EndsWithCoveredResult(Expr expression) => expression switch
            {
                Expr.Literal { Value: Atom value } => value.Equals(atom),
                Expr.Tuple { Items: [Expr.Literal { Value: Atom value }, _] } => value.Equals(tag),
                Expr.Sequence { Items.Count: > 0 } sequence => EndsWithCoveredResult(sequence.Items[^1]),
                Expr.Block block => EndsWithCoveredResult(block.Body),
                Expr.Call { Module: null, DynamicRemote: false } call => call.Function == selector.Function
                    && call.Arguments.Count == selector.Arguments.Count,
                _ => false
            };
        }
    }
}
