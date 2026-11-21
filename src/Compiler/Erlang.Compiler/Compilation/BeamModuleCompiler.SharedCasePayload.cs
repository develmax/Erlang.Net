namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private (Term Constant, string Variable)? ReadSharedCasePayload(Clause first, Clause second)
        {
            if (first is not
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Literal literal] }]
                }
                || second is not
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom sameTag }, Pattern.Variable payload] }]
                }
                || tag.Name != sameTag.Name || payload.Name == VariableScopeNames.Wildcard
                || variables.ContainsKey(payload.Name) || deferredPairMembers.ContainsKey(payload.Name))
            {
                return null;
            }

            return (literal.Value, payload.Name);
        }
    }
}
