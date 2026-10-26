namespace Erlang;

internal sealed partial class NativeDriverPortRegistry422
{
    internal async ValueTask<NativeDriverRawSendResult454> SendRaw(
        ProcessContext caller,
        Term identity,
        Term message,
        bool noSuspend,
        CancellationToken cancellation,
        DriverParentPeerTransport422 transport
    )
    {
        runtime.RequireDriverParentActor422(caller);
        // The envelope's From identifies the connected process, never the
        // sender authenticated by this actual runtime actor.
        Pid sender = caller.Self;
        bool command = RawCommandData(message) is not null;
        while (true)
        {
            Task? writable;
            bool admitted = false;
            lock (gate)
            {
                Term resolved = identity is Atom name ? runtime.WhereIs(name.Name) : identity;
                if (resolved is not PortTerm port || !ports.TryGetValue(port, out var row)
                    || row.Closing || row.StopRequested || row.PhysicallyStopped)
                    return identity is Atom ? NativeDriverRawSendResult454.RetryTarget : NativeDriverRawSendResult454.Accepted;
                cancellation.ThrowIfCancellationRequested();
                // In io.c, only recognized output retains NOSUSPEND. Its busy
                // check precedes validating iodata; other signals bypass busy.
                if (command && noSuspend && (row.ManualBusy || row.MessageBusy)) return NativeDriverRawSendResult454.WouldSuspend;
                writable = command && (row.ManualBusy || row.MessageBusy) ? row.Writable.Task : null;
                if (writable is null)
                {
                    ulong size = RawMessageBytes(message);
                    ulong queued = checked(row.QueuedMessageBytes + size);
                    _ = EnqueueSignal454(
                        row,
                        sender,
                        command,
                        () => ExecuteRaw(
                            row,
                            sender,
                            message,
                            size,
                            transport
                        )
                    );
                    row.QueuedMessageBytes = queued;
                    if (command && row.MessageLimitsEnabled && queued > row.MessageHigh)
                    {
                        row.MessageBusy = true;
                        if (row.Writable.Task.IsCompleted)
                            row.Writable = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    }
                    admitted = true;
                    writable = command && !noSuspend && row.MessageBusy ? row.Writable.Task : null;
                    if (writable is null) return NativeDriverRawSendResult454.Accepted;
                }
                if (!writable.IsCompleted) runtime.SystemMonitoring.BusyPort(sender, row.Identity);
            }
            await writable.WaitAsync(cancellation).ConfigureAwait(false);
            // Accepted work has its own lifetime and must never be resent.
            if (admitted) return NativeDriverRawSendResult454.Accepted;
            // The source send BIF retries its original registered name after
            // pre-admission suspension. Accepted signals never take this path.
            if (identity is Atom) return NativeDriverRawSendResult454.RetryTarget;
        }
    }

    private Term? RawCommandData(Term message)
    {
        if (message is TupleTerm { Items: [Pid from, TupleTerm { Items: [Atom { Name: PortNames.ProtocolCommand }, var data] }] }
            && runtime.DriverTargetInternal422(from))
            return data;

        return null;
    }

    private ulong RawMessageBytes(Term message)
    {
        if (RawCommandData(message) is not { } data) return 0;
        try
        {
            return (ulong)ErlangIoData.Flatten(data, false, false).ToArray().LongLength;
        }
        catch (ErlangException)
        {
            // The queued signal, not the sending expression, owns bad output.
            return 0;
        }
    }

    private async Task<Term> ExecuteRaw(
        NativeDriverPortRow422 row,
        Pid sender,
        Term message,
        ulong queuedBytes,
        DriverParentPeerTransport422 transport
    )
    {
        try
        {
            Term operation;
            Pid from;
            lock (gate)
            {
                if (row.Closing || row.StopRequested || row.PhysicallyStopped) return message;
                if (message is not TupleTerm { Items: [Pid source, var payload] }
                    || !runtime.DriverTargetInternal422(source) || !source.Equals(row.Connected))
                {
                    runtime.QueuePortBadSignal454(row.Identity, row.Connected);

                    return message;
                }
                from = source;
                operation = payload;
            }
            if (operation is Atom { Name: PortNames.ProtocolClose })
            {
                await Close(row, transport).ConfigureAwait(false);
                runtime.SendPortNotification(from, Term.Tuple(row.Identity, Term.A(PortNames.Closed)));
            }
            else if (operation is TupleTerm { Items: [Atom { Name: PortNames.ProtocolConnect }, Pid connected] }
                && runtime.DriverTargetInternal422(connected))
            {
                await transport.SetConnected(row.Native, connected).ConfigureAwait(false);
                lock (gate)
                {
                    if (!row.Closing && !row.StopRequested && !row.PhysicallyStopped)
                    {
                        row.Connected = connected;
                        // Raw connect changes the destination, not the links.
                        runtime.SendPortNotification(from, Term.Tuple(row.Identity, Term.A(PortNames.Connected)));
                    }
                }
            }
            else if (operation is TupleTerm { Items: [Atom { Name: PortNames.ProtocolCommand }, var data] })
                await ExecuteRawOutput(
                    row,
                    sender,
                    from,
                    data,
                    transport
                ).ConfigureAwait(false);
            else
            {
                lock (gate)
                    if (!row.Closing && !row.PhysicallyStopped)
                        runtime.QueuePortBadSignal454(row.Identity, row.Connected);
            }

            return message;
        }
        finally
        {
            lock (gate)
            {
                row.QueuedMessageBytes -= queuedBytes;
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

    private async Task ExecuteRawOutput(
        NativeDriverPortRow422 row,
        Pid sender,
        Pid from,
        Term data,
        DriverParentPeerTransport422 transport
    )
    {
        while (true)
        {
            Task? writable;
            lock (gate)
            {
                if (row.Closing || row.StopRequested || row.PhysicallyStopped) return;
                writable = row.ManualBusy ? row.Writable.Task : null;
            }
            if (writable is null) break;
            await writable.ConfigureAwait(false);
        }
        byte[] bytes;
        try
        {
            bytes = ErlangIoData.Flatten(data, false, false).ToArray();
        }
        catch (ErlangException)
        {
            lock (gate)
                if (!row.Closing && !row.PhysicallyStopped)
                    runtime.QueuePortBadSignal454(row.Identity, row.Connected);

            return;
        }
        lock (gate)
        {
            if (row.Closing || row.StopRequested || row.PhysicallyStopped) return;
            if (!from.Equals(row.Connected))
            {
                runtime.QueuePortBadSignal454(row.Identity, row.Connected);

                return;
            }
            row.OutputBytes = checked(row.OutputBytes + bytes.LongLength);
        }
        await transport.Output(
            row.Native,
            sender,
            bytes,
            CancellationToken.None
        ).ConfigureAwait(false);
    }
}
