namespace Erlang;

internal sealed record ErtsInternalResumeRecipe(
    RuntimeFunctionEntries Owner,
    LocalFunctionIdentity Identity
) : IRuntimeStoredFunctionRecipe
{
    internal const string Function = "-breakpoint/4-fun-0-";
    private const int Target = 0;
    private const int Reference = 1;

    public FunctionTerm Rebind(IReadOnlyList<Term> environment)
    {
        var function = new FunctionTerm(
            Identity,
            0,
            environment,
            async (execution, _) =>
            {
                var context = (ProcessContext)execution;
                await context.Runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ErtsInternalProtocol.Send,
                    environment[Target],
                    environment[Reference]
                );

                return Term.A(ErtsInternalProtocol.Ok);
            }
        );
        Owner.RegisterStoredSource(function, this);

        return function;
    }
}
