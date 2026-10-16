namespace Erlang;

internal static class ProcessAsyncDistributionRegistration
{
    internal static Term ChangeSelf(ProcessContext context, Term value)
    {
        if (value is not Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False } flag)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return CoreModules.Boolean(context.Runtime.ChangeAsyncDistribution(context, flag.Name == ErlangBooleanAtoms.True));
    }
}
