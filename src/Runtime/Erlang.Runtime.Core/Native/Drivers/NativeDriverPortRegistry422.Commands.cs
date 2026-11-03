namespace Erlang;

internal sealed partial class NativeDriverPortRegistry422
{
    private NativeDriverPortRow422 Require(Term identity)
    {
        if (identity is Atom name) identity = runtime.WhereIs(name.Name);

        return identity is PortTerm port && ports.TryGetValue(port, out var row)
            && !row.Closing && !row.StopRequested && row.OpenResultKnown && row.OpenSucceeded
            ? row : throw new ErlangException(ErlangErrorReasons.BadArgument);
    }

    internal async ValueTask<Term> Command(
        ProcessContext caller,
        Term identity,
        Term data,
        bool noSuspend,
        bool force,
        DriverParentPeerTransport422 transport
    )
    {
        runtime.RequireDriverParentActor422(caller);
        lock (gate)
        {
            var row = Require(identity);
            if (force && !row.DriverSoftBusy) throw new ErlangException(PortNames.NotSupported);
        }
        byte[] bytes = ErlangIoData.Flatten(data, false, false).ToArray();
        Task<Term> operation;
        while (true)
        {
            Task? admission = null;
            lock (gate)
            {
                NativeDriverPortRow422 row = Require(identity);
                if (force && !row.DriverSoftBusy)
                    throw new ErlangException(PortNames.NotSupported);
                if (noSuspend && (row.ManualBusy && !force || row.MessageBusy))
                    return Term.A(ErlangBooleanAtoms.False);
                if (row.MessageBusy || row.ManualBusy && !force) admission = row.Writable.Task;
                else
                {
                    ulong queued = checked(row.QueuedMessageBytes + (ulong)bytes.LongLength);
                    Pid sender = caller.Self;
                    operation = EnqueueSignal454(
                        row,
                        sender,
                        !force,
                        () => CommandCore(
                            row,
                            sender,
                            bytes,
                            force,
                            transport
                        )
                    );
                    row.QueuedMessageBytes = queued;
                    if (row.MessageLimitsEnabled && queued > row.MessageHigh)
                    {
                        row.MessageBusy = true;
                        if (row.Writable.Task.IsCompleted)
                            row.Writable = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    }
                    break;
                }
            }
            caller.Cancellation.ThrowIfCancellationRequested();
            await admission!.WaitAsync(caller.Cancellation).ConfigureAwait(false);
        }

        // Cancellation stops the guest wait, never abandons accepted native work.
        return await operation.WaitAsync(caller.Cancellation).ConfigureAwait(false);
    }

    private async Task<Term> CommandCore(
        NativeDriverPortRow422 row,
        Pid caller,
        byte[] bytes,
        bool force,
        DriverParentPeerTransport422 transport
    )
    {
        try
        {
            while (true)
            {
                Task? writable;
                lock (gate)
                {
                    if (row.Closing || row.StopRequested) throw new ErlangException(ErlangErrorReasons.BadArgument);
                    writable = row.ManualBusy && !force ? row.Writable.Task : null;
                    if (writable is null)
                    {
                        row.OutputBytes = checked(row.OutputBytes + bytes.LongLength);
                        runtime.Statistics.RecordOutputBytes(bytes.LongLength);
                        break;
                    }
                }
                await writable.ConfigureAwait(false);
            }
            // Packet framing belongs to spawn/fd drivers; a native output
            // callback receives the original flattened port_command bytes.
            await transport.Output(
                row.Native,
                caller,
                bytes,
                CancellationToken.None
            ).ConfigureAwait(false);

            return Term.A(ErlangBooleanAtoms.True);
        }
        finally
        {
            lock (gate)
            {
                row.QueuedMessageBytes -= (ulong)bytes.LongLength;
                if (row.MessageLimitsEnabled && row.MessageBusy
                    && (row.QueuedMessageBytes == 0 || row.QueuedMessageBytes < row.MessageLow))
                {
                    row.MessageBusy = false;
                    row.CheckMessageBusy = false;
                    if (!row.ManualBusy) row.Writable.TrySetResult();
                }
            }
        }
    }

    internal ValueTask<Term> Control(
        ProcessContext caller,
        Term identity,
        uint command,
        byte[] data,
        DriverParentPeerTransport422 transport,
        ReferenceTerm? responseReference428 = null
    )
    {
        runtime.RequireDriverParentActor422(caller);
        Task<Term> operation;
        lock (gate)
        {
            var row = Require(identity);
            Pid sender = caller.Self;
            operation = EnqueueSignal454(
                row,
                sender,
                false,
                async () =>
            {
                lock (gate) _ = Require(row.Identity);
                var result = await transport.Control(
                    row.Native,
                    sender,
                    command,
                    data,
                    CancellationToken.None
                ).ConfigureAwait(false);
                if (result.Bytes is null) return Nil.Value;

                return result.Binary ? new BitString(result.Bytes)
                    : Cons.From(result.Bytes.Select(value => Term.I(value)));
            },
                responseReference428 is null ? null : pending => ReplyInternalPort428(
                    sender,
                    row.Identity,
                    responseReference428,
                    pending,
                    false
                )
            );
        }

        return responseReference428 is null ? new(operation.WaitAsync(caller.Cancellation))
            : ValueTask.FromResult<Term>(responseReference428);
    }

    internal ValueTask<Term> Call(
        ProcessContext caller,
        Term identity,
        uint command,
        byte[] versionedEtf,
        DriverParentPeerTransport422 transport,
        ReferenceTerm? responseReference428 = null
    )
    {
        runtime.RequireDriverParentActor422(caller);
        Task<Term> operation;
        lock (gate)
        {
            var row = Require(identity);
            Pid sender = caller.Self;
            operation = EnqueueSignal454(
                row,
                sender,
                false,
                async () =>
            {
                lock (gate) _ = Require(row.Identity);
                byte[] result = await transport.Call(
                    row.Native,
                    sender,
                    command,
                    versionedEtf,
                    CancellationToken.None
                ).ConfigureAwait(false);

                return runtime.ImportExternalTerm(result).Term;
            },
                responseReference428 is null ? null : pending => ReplyInternalPort428(
                    sender,
                    row.Identity,
                    responseReference428,
                    pending,
                    true
                )
            );
        }

        return responseReference428 is null ? new(operation.WaitAsync(caller.Cancellation))
            : ValueTask.FromResult<Term>(responseReference428);
    }

    internal ValueTask<Term> CloseOrdered(
        Term identity,
        DriverParentPeerTransport422 transport,
        CancellationToken cancellation,
        ProcessContext? caller = null
    )
    {
        Task<Term> operation;
        lock (gate)
        {
            var row = Require(identity);
            operation = EnqueueSignal454(
                row,
                caller?.Self,
                false,
                async () =>
                {
                    lock (gate) _ = Require(row.Identity);
                    await Close(row, transport).ConfigureAwait(false);

                    return Term.A(ErlangBooleanAtoms.True);
                }
            );
        }

        return new(operation.WaitAsync(cancellation));
    }
}
