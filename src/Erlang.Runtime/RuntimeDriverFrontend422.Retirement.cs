namespace Erlang;

internal sealed partial class RuntimeDriverFrontend422
{
    private async Task RetireOnRequest()
    {
        await retirementRequested.Task.ConfigureAwait(false);
        var failures = new List<Exception>();
        lock (gate) if (fatal is not null) failures.Add(fatal);
        Task drain = DrainNativeOwners();
        using var bound = new CancellationTokenSource(configuration.Launch.ShutdownTimeout);
        try
        {
            // A dead child cannot finish a retained native initialization.
            // Join physical cleanup below before faulting its waiting owners.
            Task ended = await Task.WhenAny(drain, child.PhysicalExit).WaitAsync(bound.Token).ConfigureAwait(false);
            await ended.ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        // Child.Dispose requests real native Shutdown outside any retained
        // normal permit, drains ordered events, joins physical process exit;
        // its bounded fault path kills only this actually owned child.
        try
        {
            await child.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        // Successful-only source fence. If exit cannot be proven, this owner
        // and both leases stay retained and shutdown is not reported settled.
        await child.PhysicalExit.ConfigureAwait(false);
        try
        {
            await child.PhysicalCleanup.ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        if (!drain.IsCompletedSuccessfully)
        {
            var failure = new IOException(DriverParentWireNames422.Closed);
            ports.ChildPhysicallySettled(failure);
            drivers.ChildPhysicallySettled(failure);
        }
        try
        {
            await drain.ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            await monitors.BeginClose().ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            await ports.DrainOperations().ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            await supervision.ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            actors.Dispose();
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        // No env snapshot or reopened global state: native finish has retained
        // the actual owner/tree/locale until the physical child fence above.
        try
        {
            environment.Dispose();
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            dispatcher.Dispose();
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }

    private async Task DrainNativeOwners()
    {
        var failures = new List<Exception>();
        try
        {
            await ports.CloseAll(transport).ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            await drivers.BeginClose().ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
