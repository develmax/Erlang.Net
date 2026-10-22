namespace Erlang;

internal static class DistributionStateNativeBridge
{
    internal static void Register(ModuleRegistry registry)
    {
        registry.Register(
            DistributionPendingNativeNames.Module,
            DistributionPendingNativeNames.GetCreation,
            0,
            (context, _) =>
            {
                context.Cancellation.ThrowIfCancellationRequested();

                return ValueTask.FromResult(context.Runtime.ReadNativeCreation());
            }
        );
        registry.Register(
            DistributionPendingNativeNames.Module,
            DistributionPendingNativeNames.AbortPendingConnection,
            2,
            (context, args) => ValueTask.FromResult(Abort(context, args[0], args[1]))
        );
    }

    private static Term Abort(ProcessContext context, Term node, Term handle)
    {
        context.Cancellation.ThrowIfCancellationRequested();
        if (node is not Atom remoteNode) throw new ErlangException(ErlangErrorReasons.BadArgument);
        try
        {
            return Term.A(context.Runtime.DistributionPending.AbortPendingConnectionNative(remoteNode, handle)
                ? CommonAtomNames.True : CommonAtomNames.False);
        }
        catch (ArgumentException)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
    }
}
