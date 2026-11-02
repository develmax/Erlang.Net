namespace Erlang;

public sealed partial class ProcessRuntime
{
    // Init death owns a whole-VM batch even when init has no sequential token.
    // The sentinel adds no token tuple or process-word credit.
    private ValueTask<PreparedSequenceTermination?> PrepareInitializationShutdownSentinel(
        ProcessContext context,
        Term reason,
        Pid? removedLink,
        ProcessLinkIdentity? removedOrigin
    )
    {
        SequenceTerminationSnapshot snapshot;
        lock (gate)
        {
            if (!processes.TryGetValue(context.Self, out ProcessContext? current) || !ReferenceEquals(current, context)) return ValueTask.FromResult<PreparedSequenceTermination?>(null);
            if (NeedsSingleSequenceTerminationPreparation(context, removedLink, removedOrigin)) return ValueTask.FromResult<PreparedSequenceTermination?>(null);
            Pid[] beforeLinks = context.Links.ToArray();
            Pid[] links = removedLink is null ? beforeLinks : beforeLinks.Where(pid => !pid.Equals(removedLink)).ToArray();
            Pid[] beforeOrigins = context.PendingLinkOrigins.Values.ToArray();
            Pid[] origins = removedOrigin is null ? beforeOrigins
                : context.PendingLinkOrigins.Where(pair => !ReferenceEquals(pair.Key, removedOrigin)).Select(pair => pair.Value).ToArray();
            SequenceTraceToken? token = context.SequentialTrace.Token;
            snapshot = new(
                token,
                null,
                context.SequentialTrace.Clock,
                context.SequentialTrace.LastCount,
                beforeLinks,
                links,
                beforeOrigins,
                origins,
                links.Concat(origins).Select(SnapshotPid).ToArray(),
                links.Concat(origins).Select(processes.ContainsKey).ToArray(),
                links.Concat(origins).Select(pid => processes.GetValueOrDefault(pid)).ToArray()
            );
        }
        RuntimeProcessPayload641 prepared = RuntimeProcessPayload641.Prepare641(this, null, reason);
        try
        {
            return ValueTask.FromResult<PreparedSequenceTermination?>(new(
                context,
                snapshot,
                prepared.Value641,
                prepared,
                null,
                new PreparedSequenceTraceOutput?[snapshot.Count],
                new PreparedMailboxMessage?[snapshot.Count]
            ));
        }
        catch
        {
            prepared.Close641();
            throw;
        }
    }
}
