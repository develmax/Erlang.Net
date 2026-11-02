namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal async ValueTask<PreparedSequenceTermination?> PrepareSequenceTerminationAsync(
        ProcessContext context,
        Term reason,
        Pid? removedLink = null,
        ProcessLinkIdentity? removedOrigin = null
    )
    {
        if (!context.Self.Equals(InitializationPid))
            return await PrepareSingleSequenceTerminationAsync(
                context,
                reason,
                removedLink,
                removedOrigin
            ).ConfigureAwait(false);
        lock (gate)
            if (disposed || IsHaltRequested || !processes.ContainsKey(context.Self)) return null;
        PreparedSequenceShutdownBatch batch = await PrepareSequenceShutdownAsync(
            context,
            reason,
            removedLink,
            removedOrigin
        ).ConfigureAwait(false);
        try
        {
            bool gone;
            lock (gate) gone = IsHaltRequested || !processes.TryGetValue(context.Self, out ProcessContext? current)
                || !ReferenceEquals(current, context);
            if (gone)
            {
                await batch.DisposeAsync().ConfigureAwait(false);

                return null;
            }
            PreparedSequenceTermination? own = batch.For(context);
            if (own is null)
            {
                own = await PrepareInitializationShutdownSentinel(
                    context,
                    reason,
                    removedLink,
                    removedOrigin
                ).ConfigureAwait(false);
                if (own is null)
                {
                    await batch.DisposeAsync().ConfigureAwait(false);

                    return null;
                }
                for (int index = 0; index < batch.Contexts.Length; index++)
                    if (ReferenceEquals(batch.Contexts[index], context)) batch.Plans[index] = own;
            }
            own.ShutdownBatch = batch;

            return own;
        }
        catch (Exception primary)
        {
            try
            {
                await batch.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(primary, cleanup);
            }
            throw;
        }
    }

    private async ValueTask<PreparedSequenceTermination?> PrepareSingleSequenceTerminationAsync(
        ProcessContext context,
        Term reason,
        Pid? removedLink = null,
        ProcessLinkIdentity? removedOrigin = null
    )
    {
        AssertSequenceTracePreparation();
        SequenceTerminationSnapshot snapshot;
        ProcessTermBorrow506? physical;
        PreparedSequenceTraceTransport641? pending;
        lock (gate)
        {
            if (!processes.TryGetValue(context.Self, out ProcessContext? current) || !ReferenceEquals(current, context)) return null;
            Pid[] admissionLinks = context.Links.ToArray();
            Pid[] links = removedLink is null ? admissionLinks : admissionLinks.Where(pid => !pid.Equals(removedLink)).ToArray();
            Pid[] admissionOrigins = context.PendingLinkOrigins.Values.ToArray();
            Pid[] origins = removedOrigin is null ? admissionOrigins
                : context.PendingLinkOrigins.Where(pair => !ReferenceEquals(pair.Key, removedOrigin)).Select(pair => pair.Value).ToArray();
            SequenceTraceToken? currentToken = context.SequentialTrace.Token;
            SequenceTraceToken? tokenValue = currentToken is null ? null
                : new(
                    currentToken.Flags,
                    currentToken.Label,
                    currentToken.Serial,
                    currentToken.Sender,
                    currentToken.LastCount
                );
            snapshot = new(
                currentToken,
                tokenValue,
                context.SequentialTrace.Clock,
                context.SequentialTrace.LastCount,
                admissionLinks,
                links,
                admissionOrigins,
                origins,
                links.Concat(origins).Select(SnapshotPid).ToArray(),
                links.Concat(origins).Select(processes.ContainsKey).ToArray(),
                links.Concat(origins).Select(pid => processes.GetValueOrDefault(pid)).ToArray()
            );
            if (snapshot.Token is null || snapshot.Count == 0) return null;
            physical = context.SequentialTrace.BorrowPhysical();
            PreparedSequenceTraceTransport641? existingPending = context.SequentialTrace.Pending;
            pending = existingPending?.ForkMetadata(
                existingPending.Token.Flags,
                existingPending.Token.Serial,
                existingPending.Token.Sender,
                existingPending.Token.LastCount
            );
        }
        using (physical)
        using (pending)
        {
            PreparedSequenceTraceTransport641? token = null;
            RuntimeProcessPayload641? reason641 = null;
            var output = new PreparedSequenceTraceOutput?[snapshot.Count];
            var signals = new PreparedMailboxMessage?[snapshot.Count];
            try
            {
                Term portableReason = reason;
                if (snapshot.Value is { } original && snapshot.Count != 0)
                {
                    // Tracer()==dying process is cleared after process removal.
                    // LinkedExit therefore advances even when the old tracer was self.
                    SequenceTraceToken first = original with
                    {
                        Sender = context.Self,
                        Serial = SequenceTraceParameters.Signed(unchecked(snapshot.Clock + 1)),
                        LastCount = SequenceTraceParameters.Signed(snapshot.LastCount)
                    };
                    token = PreparedSequenceTraceTransport641.Capture(this, first);
                    reason641 = RuntimeProcessPayload641.Prepare641(this, null, reason);
                    portableReason = reason641.Value641;
                    using ModuleLiteralRoots801? exitLiterals = ModuleLiteralRoots801.RetainCombined801(
                        [token.LiteralRoots814, reason641.Graph641?.LiteralRoots801]);
                    for (int index = 0; index < signals.Length; index++)
                    {
                        if (snapshot.Recipients[index] is not { } recipient) continue;
                        Term exitReason = snapshot.UsesExitReason(index) ? portableReason : Term.A(ErlangErrorReasons.NoProcess);
                        SequenceTraceToken exitToken = token.Token with
                        {
                            Serial = SequenceTraceParameters.Signed(unchecked(snapshot.Clock + (ulong)index + 1))
                        };
                        try
                        {
                            signals[index] = await MailboxRoots593.PrepareMailboxMessageAsync(
                                this,
                                recipient,
                                ProcessMessageQueueDataMode411.OffHeap,
                                Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), context.Self, exitReason),
                                exitToken,
                                exitLiterals
                            ).ConfigureAwait(false);
                        }
                        catch (ObjectDisposedException)
                        {
                            lock (gate)
                            {
                                if (processes.TryGetValue(snapshot.Destination(index), out ProcessContext? current)
                                    && ReferenceEquals(current, recipient)) throw;
                                snapshot.LiveDestinations[index] = false;
                            }
                        }
                    }
                    if ((first.Flags & SequenceTraceFlags.Send) != 0)
                        for (int index = 0; index < output.Length; index++)
                        {
                            if (!snapshot.LiveDestinations[index]) continue;
                            Term exitReason = snapshot.UsesExitReason(index) ? portableReason : Term.A(ErlangErrorReasons.NoProcess);
                            output[index] = await SequenceTracing.PrepareOutputAsync(
                                [token.Token.Label, snapshot.Destination(index), exitReason], context.Self
                            ).ConfigureAwait(false);
                        }
                }

                return new(
                    context,
                    snapshot,
                    portableReason,
                    reason641,
                    token,
                    output,
                    signals
                );
            }
            catch (Exception primary)
            {
                var failed = new PreparedSequenceTermination(
                    context,
                    snapshot,
                    reason,
                    reason641,
                    token,
                    output,
                    signals
                );
                try
                {
                    await failed.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(primary, cleanup);
                }
                throw;
            }
        }
    }

    internal bool NeedsSequenceTerminationPreparation(ProcessContext context, Pid? removedLink = null, ProcessLinkIdentity? removedOrigin = null)
        => context.Self.Equals(InitializationPid) && pendingShutdownSequence is null && !disposed && !IsHaltRequested
            || NeedsSingleSequenceTerminationPreparation(context, removedLink, removedOrigin);

    private bool NeedsSingleSequenceTerminationPreparation(ProcessContext context, Pid? removedLink = null, ProcessLinkIdentity? removedOrigin = null)
    {
        if (context.SequentialTrace.Token is null) return false;

        return context.PendingLinkOrigins.Any(pair => removedOrigin is null || !ReferenceEquals(pair.Key, removedOrigin))
            || context.Links.Any(pid => removedLink is null || !pid.Equals(removedLink));
    }

    private Pid SnapshotPid(Pid value)
    {
        if (ReferenceEquals(value.LocalIdentity, LocalIdentity)) return value;
        LocalNodeSnapshot wire = value.WireIdentity;

        return new Pid(wire.Node, value.Id, wire.Creation);
    }

    internal bool IsCurrentSequenceTermination(PreparedSequenceTermination prepared, bool beforeAdmission = false)
    {
        lock (gate)
        {
            ProcessContext context = prepared.Process;
            SequenceTerminationSnapshot snapshot = prepared.Snapshot;
            if (!processes.TryGetValue(context.Self, out ProcessContext? current) || !ReferenceEquals(current, context)
                || !ReferenceEquals(context.SequentialTrace.Token, snapshot.Token)
                || context.SequentialTrace.Clock != snapshot.Clock || context.SequentialTrace.LastCount != snapshot.LastCount
                || !context.Links.SequenceEqual(beforeAdmission ? snapshot.AdmissionLinks : snapshot.Links, ReferenceEqualityComparer.Instance)
                || !context.PendingLinkOrigins.Values.SequenceEqual(beforeAdmission ? snapshot.AdmissionOrigins : snapshot.PendingOrigins, ReferenceEqualityComparer.Instance)) return false;
            // A new local destination cannot appear with the same PID, but a
            // previously live one may die. Preparing an event is then unused;
            // original LinkedExit still advances before checking its existence.
            for (int index = 0; index < snapshot.Count; index++)
                if (!snapshot.LiveDestinations[index] && processes.ContainsKey(snapshot.Destination(index))) return false;

            return prepared.AreOutputPlansCurrent();
        }
    }
}
