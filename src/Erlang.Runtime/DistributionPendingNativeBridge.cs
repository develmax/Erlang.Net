namespace Erlang;

internal static class DistributionPendingNativeBridge
{
    internal static void Register(ModuleRegistry registry)
    {
        registry.Register(
            DistributionPendingNativeNames.Module,
            DistributionPendingNativeNames.NewConnection,
            1,
            (context, arguments) => ValueTask.FromResult(NewConnection(context, arguments[0]))
        );
        DistributionStateNativeBridge.Register(registry);
    }

    private static Term NewConnection(ProcessContext context, Term value)
    {
        context.Cancellation.ThrowIfCancellationRequested();
        if (value is not Atom node)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        try
        {
            return context.Runtime.DistributionPending.BeginPending(node);
        }
        catch (ArgumentException)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        catch (InvalidOperationException failure) when (failure.Message == DistributionPendingDiagnostics.Capacity)
        {
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
        }
    }
}
