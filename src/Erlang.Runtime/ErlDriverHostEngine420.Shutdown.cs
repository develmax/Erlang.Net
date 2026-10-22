namespace Erlang;

internal sealed partial class ErlDriverHostEngine420
{
    private async Task RetirePort(ErlDriverPort420 port)
    {
        // Never wait for normal queued cleanup while its native stop callback
        // still owns the parent queue permit that admits those cleanup jobs.
        await Task.Yield();
        var failures = new List<Exception>();
        try
        {
            if (timers is not null) await timers.RemovePortAsync(port.NativePort).ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            if (select is not null) await select.RemovePortAsync(port.NativePort).ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            monitors?.RemovePort(port.NativePort);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            pdl?.ReleasePort(port.NativePort);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        lock (gate)
        {
            ports.Remove(port.Identity.Port);
            termPorts.Remove(port.PortTerm);
        }
        try
        {
            await peer.SendEventAsync(Event(
                ErlDriverHostOperation420.PortStopped,
                port.Identity,
                ErlDriverPrimitiveWire420.Encode(new ErlDriverStopped420(failures.Count == 0 ? 0 : -1))
            )).ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        if (failures.Count == 0) return;
        var aggregate = new AggregateException(failures);
        fault.Record(aggregate);
        throw aggregate;
    }

    internal Task CloseAsync()
    {
        lock (gate)
        {
            if (close is not null) return close;
            closing = true;
            bool suppress = !ExecutionContext.IsFlowSuppressed();
            AsyncFlowControl flow = default;
            try
            {
                if (suppress) flow = ExecutionContext.SuppressFlow();
                close = CloseCore();
            }
            finally
            {
                if (suppress) flow.Undo();
            }

            return close;
        }
    }

    private async Task CloseCore()
    {
        await Task.Yield();
        var failures = new List<Exception>();
        ErlDriverPort420[] owned;
        lock (gate) owned = ports.Values.ToArray();
        foreach (var port in owned)
        {
            try
            {
                if (!port.Stopped && jobs is not null)
                    await jobs.Post(port.Identity, ErlDriverPortTaskKind420.Stop, _ =>
                    {
                        port.RequestStop();

                        return true;
                    }).ConfigureAwait(false);
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
        }
        Task[] portDrains;
        lock (gate) portDrains = retirement.ToArray();
        try
        {
            await Task.WhenAll(portDrains).ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            if (timers is not null) await timers.CloseAsync().ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            if (select is not null) await select.CloseAsync().ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            monitors?.Close();
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            if (async is not null) await async.CloseAsync().ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            if (jobs is not null) await jobs.CloseAsync().ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        bool imagesClosed = false;
        try
        {
            await images.CloseAsync().ConfigureAwait(false);
            imagesClosed = true;
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            linked?.RequireDrained();
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            pdl?.RequireDrained();
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        if (imagesClosed)
        {
            try
            {
                if (bridge is not null) await bridge.CloseServicesAsync().ConfigureAwait(false);
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
            try
            {
                table?.Dispose();
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
            try
            {
                async?.NativeOwner?.Dispose();
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
            try
            {
                bridge?.Dispose();
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
            try
            {
                primitive?.Dispose();
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
            try
            {
                tokens.Dispose();
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
        }
        if (table?.ReleaseFailure is { } releaseFailure) failures.Add(releaseFailure);
        if (primitive?.Failure is { } primitiveFailure) failures.Add(primitiveFailure);
        if (primitive?.ReleaseWindowsError is > 0)
            failures.Add(new System.ComponentModel.Win32Exception(primitive.ReleaseWindowsError));
        if (fault.Failure is { } serviceFailure) failures.Add(serviceFailure);
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
