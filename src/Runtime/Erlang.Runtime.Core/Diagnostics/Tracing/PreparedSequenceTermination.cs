namespace Erlang;

// This owner is detached BEFORE context641 is sealed or the state frame closes.
// It never contains a context-bound641 work item or a sender frame after Prepare.
internal sealed class PreparedSequenceTermination : IAsyncDisposable
{
    internal ProcessContext Process { get; }
    internal SequenceTerminationSnapshot Snapshot { get; }
    internal Term Reason { get; }
    internal RuntimeProcessPayload641? Reason641 { get; }
    internal PreparedSequenceTraceTransport641? Token641 { get; }
    private readonly PreparedSequenceTraceOutput?[] output;
    private readonly PreparedMailboxMessage?[] signals;
    private readonly bool ownsSignals;
    private readonly bool ownsOutputs;
    private readonly PreparedSequenceTermination? predecessor;
    private int next;
    private int closed;
    private int transferred;
    internal bool IsTransferred => Volatile.Read(ref transferred) != 0;
    internal PreparedSequenceShutdownBatch? ShutdownBatch { get; set; }
    internal TaskCompletionSource FanoutRetired { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal void TransferToRuntime()
    {
        if (predecessor is { IsTransferred: false }) predecessor.TransferToRuntime();
        if (Interlocked.Exchange(ref transferred, 1) != 0)
            throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
    }

    internal PreparedSequenceTermination(
        ProcessContext process,
        SequenceTerminationSnapshot snapshot,
        Term reason,
        RuntimeProcessPayload641? reason641,
        PreparedSequenceTraceTransport641? token641,
        PreparedSequenceTraceOutput?[] output,
        PreparedMailboxMessage?[] signals,
        bool ownsSignals = true,
        bool ownsOutputs = true,
        PreparedSequenceTermination? predecessor = null
    )
    {
        Process = process;
        Snapshot = snapshot;
        Reason = reason;
        Reason641 = reason641;
        Token641 = token641;
        this.output = output;
        this.signals = signals;
        this.ownsSignals = ownsSignals;
        this.ownsOutputs = ownsOutputs;
        this.predecessor = predecessor;
    }

    internal bool IsCurrentBeforeTermination()
        => Process.Runtime.IsCurrentSequenceTermination(this);

    internal bool IsCurrentBeforeAdmission()
        => Process.Runtime.IsCurrentSequenceTermination(this, beforeAdmission: true);

    internal bool AreOutputPlansCurrent()
    {
        if (Token641 is null) return true;
        for (int index = 0; index < output.Length; index++)
        {
            if (!Snapshot.LiveDestinations[index]) continue;
            if (!Process.Runtime.SequenceTracing.IsCurrentOutputPlanOrSuppressed(
                output[index],
                Token641.Token,
                SequenceTraceFlags.Send,
                Process.Self
            )) return false;
        }

        return true;
    }

    internal SequenceTraceToken? ConsumeNext(
        Pid destination,
        Term reason,
        out PreparedSequenceTraceOutput? plan,
        out PreparedMailboxMessage? signal
    )
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref closed) != 0, this);
        if (next >= Snapshot.Count || !Snapshot.Destination(next).Equals(destination)
            || (Snapshot.UsesExitReason(next) ? !ReferenceEquals(reason, Reason)
                : reason is not Atom { Name: ErlangErrorReasons.NoProcess }))
            throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
        int index = next++;
        plan = output[index];
        signal = signals[index];
        if (Token641 is null) return null;

        return Token641.Token with
        {
            Serial = SequenceTraceParameters.Signed(unchecked(Snapshot.Clock + (ulong)index + 1)),
            LastCount = SequenceTraceParameters.Signed(Snapshot.LastCount)
        };
    }

    internal Term ReasonFor(int index) => Snapshot.UsesExitReason(index) ? Reason : Term.A(ErlangErrorReasons.NoProcess);

    // R held. All surviving arbitrary leaves and their actual packet cells
    // already exist; only event positions and bounded serial fields change.
    internal PreparedSequenceTermination ProjectShutdown(Pid[] links, Pid[] origins, int[] selected)
    {
        var projected = new SequenceTerminationSnapshot(
            Snapshot.Token,
            Snapshot.Value,
            Snapshot.Clock,
            Snapshot.LastCount,
            links,
            links,
            origins,
            origins,
            selected.Select(index => Snapshot.Destinations[index]).ToArray(),
            selected.Select(index => Snapshot.LiveDestinations[index]).ToArray(),
            selected.Select(index => Snapshot.Recipients[index]).ToArray()
        );
        var plans = selected.Select(index => output[index]).ToArray();
        var packets = selected.Select(index => signals[index]).ToArray();
        PreparedSequenceTraceTransport641? token = null;
        RuntimeProcessPayload641? reason = null;
        try
        {
            if (Token641 is { } original)
            {
                SequenceTraceToken value = original.Token;
                token = original.ForkMetadata(
                    value.Flags,
                    value.Serial,
                    value.Sender,
                    value.LastCount
                );
            }
            reason = Reason641?.Borrow641(null);
            for (int index = 0; index < packets.Length; index++)
                if (packets[index]?.Token is { PhysicalTuple: { } tuple } packet)
                {
                    var block = (ProcessTermSequence506)ProcessTermHeap506.Storage(tuple)!;
                    var heap = (ProcessTermHeap506)block.Owner;
                    heap.RewriteSequenceToken(tuple, packet, packet with
                    {
                        Serial = SequenceTraceParameters.Signed(unchecked(Snapshot.Clock + (ulong)index + 1))
                    });
                }

            return new(
                Process,
                projected,
                Reason,
                reason,
                token,
                plans,
                packets,
                ownsSignals: false,
                ownsOutputs: false,
                predecessor: this
            );
        }
        catch
        {
            token?.Dispose();
            reason?.Close641();
            throw;
        }
    }

    // Existing asynchronous storage retirement can change the tracer before
    // FinishTermination. Rebuild only event frames at that real pre-R seam;
    // token/reason custody stays independently alive throughout the transfer.
    internal async ValueTask<PreparedSequenceTermination> RefreshOutputsAsync()
    {
        Process.Runtime.AssertSequenceTracePreparation();
        PreparedSequenceTraceTransport641? token = null;
        RuntimeProcessPayload641? reason = null;
        var plans = new PreparedSequenceTraceOutput?[Snapshot.Count];
        try
        {
            if (Token641 is { } oldToken)
            {
                SequenceTraceToken value = oldToken.Token;
                token = oldToken.ForkMetadata(
                    value.Flags,
                    value.Serial,
                    value.Sender,
                    value.LastCount
                );
                reason = Reason641?.Borrow641(null);
                if ((value.Flags & SequenceTraceFlags.Send) != 0)
                    for (int index = 0; index < plans.Length; index++)
                        if (Snapshot.LiveDestinations[index])
                            plans[index] = await Process.Runtime.SequenceTracing.PrepareOutputAsync(
                                [value.Label, Snapshot.Destination(index), ReasonFor(index)], Process.Self
                            ).ConfigureAwait(false);
            }

            return new(
                Process,
                Snapshot,
                Reason,
                reason,
                token,
                plans,
                signals,
                ownsSignals: false
            );
        }
        catch (Exception primary)
        {
            var failed = new PreparedSequenceTermination(
                Process,
                Snapshot,
                Reason,
                reason,
                token,
                plans,
                signals,
                ownsSignals: false
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

    public ValueTask DisposeAsync() => Volatile.Read(ref transferred) != 0 ? ValueTask.CompletedTask
        : ShutdownBatch is { IsTransferred: false } batch ? batch.DisposeAsync() : RetireAsync();

    internal async ValueTask RetireAsync()
    {
        if (Interlocked.Exchange(ref closed, 1) != 0)
        {
            await FanoutRetired.Task.ConfigureAwait(false);

            return;
        }
        var failures = new List<Exception>();
        if (ownsOutputs)
            foreach (PreparedSequenceTraceOutput? plan in output)
            {
                try
                {
                    if (plan is not null) await plan.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            }
        if (ownsSignals)
            foreach (PreparedMailboxMessage? signal in signals)
            {
                try
                {
                    if (signal is not null) await signal.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            }
        Task tokenCompletion = Token641?.Payload641.Completion641 ?? Task.CompletedTask;
        Task reasonCompletion = Reason641?.Completion641 ?? Task.CompletedTask;
        try
        {
            Token641?.Dispose();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        try
        {
            Reason641?.Close641();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        try
        {
            await Task.WhenAll(tokenCompletion, reasonCompletion).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        if (IsTransferred && predecessor is not null)
            try
            {
                await predecessor.RetireAsync().ConfigureAwait(false);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        if (failures.Count != 0)
        {
            var failure = new AggregateException(failures);
            FanoutRetired.TrySetException(failure);
            throw failure;
        }
        FanoutRetired.TrySetResult();
    }
}
