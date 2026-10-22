namespace Erlang;

internal sealed record DebugDirtyExternalFunctionRecipe424(
    RuntimeFunctionEntries Owner,
    ExternalFunctionIdentity Identity
) : IRuntimeStoredFunctionRecipe
{
    public FunctionTerm Rebind(IReadOnlyList<Term> environment)
    {
        if (environment.Count != 0)
            throw new InvalidOperationException(RuntimeEtsFoundationNames.UnsupportedFunction);

        var function = new FunctionTerm(
            Identity,
            (context, arguments) => ((ProcessContext)context).Runtime.Modules.Call(
                (ProcessContext)context,
                Identity.Module.Name,
                Identity.Function.Name,
                arguments.ToArray()
            )
        );
        Owner.RegisterStoredSource(function, this);

        return function;
    }
}
