namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private EntryCall? ReadPreparedCasePayloadCall(Clause clause, Pattern payload, int register)
        {
            if (clause.Guard is not null || payload is not Pattern.Variable variable
                || variable.Name == VariableScopeNames.Wildcard || variables.ContainsKey(variable.Name)
                || deferredPairMembers.ContainsKey(variable.Name)
                || clause.Body is not Expr.Case { Value: Expr.Call call } continuation
                || ReadUsedEntryParameters(new Clause([variable], null,
                    continuation with { Value = new Expr.Literal(Nil.Value) })) is not { Count: 0 })
            {
                return null;
            }

            string[] parameters = Enumerable.Repeat(VariableScopeNames.Wildcard, register + 1).ToArray();
            parameters[register] = variable.Name;
            string[] saved = Enumerable.Repeat(
                VariableScopeNames.Wildcard,
                variables.Values.DefaultIfEmpty(-1).Max() + 1
            ).ToArray();
            foreach (var (name, slot) in variables)
            {
                saved[slot] = name;
            }

            return ReadEntryCall(call, parameters, saved);
        }
    }
}
