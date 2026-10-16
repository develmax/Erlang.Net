namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly Dictionary<ProcessContext, ProcessExitContinuation> exitContinuations = new();
    private readonly List<Exception> exitContinuationFailures = new();

    internal bool OwnsExitContinuation(ProcessExitContinuation continuation)
    {
        lock (gate)
            return exitContinuations.TryGetValue(continuation.Process, out var owned) && ReferenceEquals(owned, continuation);
    }

    internal async Task DrainExitContinuations()
    {
        while (true)
        {
            Task[] current;
            lock (gate)
            {
                current = exitContinuations.Values.Select(item => item.Quiescence.Task).ToArray();
                if (current.Length == 0)
                {
                    if (exitContinuationFailures.Count != 0)
                        throw new AggregateException(exitContinuationFailures);

                    return;
                }
            }
            try
            {
                await Task.WhenAll(current).ConfigureAwait(false);
            }
            catch
            {
                // Each completion records its failure before removal. Drain every owned continuation first.
            }
        }
    }

    private void StartExitContinuation(ProcessExitContinuation continuation)
    {
        exitContinuations.Add(continuation.Process, continuation);
        _ = CompleteExitContinuation(continuation);
    }

    private async Task CompleteExitContinuation(ProcessExitContinuation continuation)
    {
        Exception? failure = null;
        bool localCleanupCompleted = false;
        try
        {
            await Task.Yield();
            await continuation.LocalCleanup.Task.ConfigureAwait(false);
            localCleanupCompleted = true;
            var failures = new List<Exception>();
            foreach (var output in continuation.Output)
            {
                try
                {
                    await output.Entry.Owner.PublishTerminalControl(output, continuation).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
                continuation.NextOutput++;
            }
            if (failures.Count != 0)
                throw new AggregateException(failures);
        }
        catch (Exception exception)
        {
            failure = exception;
            if (!localCleanupCompleted)
            {
                var failures = new List<Exception> { exception };
                foreach (var output in continuation.Output)
                {
                    try
                    {
                        output.Entry.Owner.RetireFailedOutputGeneration(output.Entry, output.Connection, output.ConnectionId);
                    }
                    catch (Exception retirementFailure)
                    {
                        failures.Add(retirementFailure);
                    }
                }
                failure = new AggregateException(failures);
            }
        }
        finally
        {
            lock (gate)
            {
                exitContinuations.Remove(continuation.Process);
                if (failure is null)
                {
                    continuation.Process.Done.TrySetResult(continuation.Reason);
                    continuation.Quiescence.TrySetResult();
                }
                else
                {
                    exitContinuationFailures.Add(failure);
                    continuation.Process.Done.TrySetException(failure);
                    continuation.Quiescence.TrySetException(failure);
                }
            }
        }
    }

    private IReadOnlyList<DistributionExitOutput> CaptureDistributionExitOutput(ProcessContext process, Term reason)
    {
        var output = new List<DistributionExitOutput>();
        var spawnMonitorOutput = new List<DistributionExitOutput>();
        foreach (var link in distributionProcessLinks.Where(item => ReferenceEquals(item.Local, process)).ToArray())
        {
            distributionProcessLinks.Remove(link);
            bool active = link.Established && process.Links.Contains(link.Remote) &&
                process.LinkIdentities.TryGetValue(link.Remote, out var identity) && ReferenceEquals(identity, link.Identity);
            if (active || process.PendingLinkOrigins.ContainsKey(link.Identity))
                output.Add(new(
                    link.Entry,
                    link.Connection,
                    link.ConnectionId,
                    Term.Tuple(
                        Term.I(DistributionControlOpcodes.Exit),
                        process.Self,
                        link.Remote,
                        active ? reason : Term.A(ErlangErrorReasons.NoProcess)
                    ),
                    process.Self
                ));
        }
        foreach (var monitor in distributionProcessMonitors.Where(item => ReferenceEquals(item.Local, process)).ToArray())
        {
            distributionProcessMonitors.Remove(monitor);
            if ((monitor.Connection.Flags & DistributionPassThroughFlags.ProcessMonitors) != DistributionPassThroughFlags.ProcessMonitors)
                continue;
            TupleTerm control = monitor.Origin
                ? Term.Tuple(
                    Term.I(DistributionControlOpcodes.Demonitor),
                    process.Self,
                    monitor.Remote,
                    monitor.Reference
                )
                : Term.Tuple(
                    Term.I(DistributionControlOpcodes.MonitorExit),
                    monitor.Watched,
                    monitor.Remote,
                    monitor.Reference,
                    monitor.Established ? reason : Term.A(ErlangErrorReasons.NoProcess)
                );
            var target = monitor.Origin && monitor.SpawnOptions ? spawnMonitorOutput : output;
            target.Add(new(
                monitor.Entry,
                monitor.Connection,
                monitor.ConnectionId,
                control,
                process.Self
            ));
        }

        // Native origin monitors retire in MONITORS, before deferred distribution links.
        return spawnMonitorOutput.Concat(output).ToArray();
    }
}
