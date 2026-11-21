namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool HasCoveredTaggedCallPair(Expr.Case decision)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || decision.Value is not Expr.Call { Module: not null, DynamicRemote: false }
                || decision.Clauses.All(clause => clause.Guard is null))
            {
                return false;
            }

            var covered = new Dictionary<Atom, bool>();
            foreach (Clause clause in decision.Clauses)
            {
                if (clause.Patterns is not [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, var payload] }])
                {
                    return false;
                }

                // A tag must have a final irrefutable payload arm. It must not
                // need the original pair again if an earlier guard rejects.
                covered[tag] = clause.Guard is null && (payload is Pattern.Any
                    || payload is Pattern.Variable variable && (variable.Name == VariableScopeNames.Wildcard
                        || !variables.ContainsKey(variable.Name) && !deferredPairMembers.ContainsKey(variable.Name)));
            }

            return covered.Count > 1 && covered.Values.All(complete => complete);
        }
    }
}
