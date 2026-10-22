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

    private static async Task<Term> After(Task previous, Task accepted, Func<Task<Term>> operation)
    {
        await accepted.ConfigureAwait(false);
        try
        {
            await previous.ConfigureAwait(false);
        }
        catch (ErlangException) { /* A prior source badarg does not poison signals. */ }

        return await operation().ConfigureAwait(false);
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
                if (row.MessageBusy) admission = row.Writable.Task;
                else
                {
                    ulong queued = checked(row.QueuedMessageBytes + (ulong)bytes.LongLength);
                    var accepted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    Pid sender = caller.Self;
                    operation = After(row.Commands, accepted.Task, () => CommandCore(
                        row,
                        sender,
                        bytes,
                        force,
                        transport
                    ));
                    try
                    {
                        Track(operation);
                    }
                    catch
                    {
                        accepted.TrySetCanceled();
                        throw;
                    }
                    row.Commands = operation;
                    row.QueuedMessageBytes = queued;
                    if (row.MessageLimitsEnabled && queued > row.MessageHigh)
                    {
                        row.MessageBusy = true;
                        if (row.Writable.Task.IsCompleted)
                            row.Writable = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    }
                    accepted.TrySetResult();
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
            var accepted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Pid sender = caller.Self;
            operation = After(
                row.Commands,
                accepted.Task,
                async () =>
            {
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
            }
            );
            if (responseReference428 is not null)
                operation = ReplyInternalPort428(
                    sender,
                    row.Identity,
                    responseReference428,
                    operation,
                    false
                );
            try
            {
                Track(operation);
            }
            catch
            {
                accepted.TrySetCanceled();
                throw;
            }
            row.Commands = operation;
            accepted.TrySetResult();
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
            var accepted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Pid sender = caller.Self;
            operation = After(
                row.Commands,
                accepted.Task,
                async () =>
            {
                byte[] result = await transport.Call(
                    row.Native,
                    sender,
                    command,
                    versionedEtf,
                    CancellationToken.None
                ).ConfigureAwait(false);

                return runtime.ImportExternalTerm(result).Term;
            }
            );
            if (responseReference428 is not null)
                operation = ReplyInternalPort428(
                    sender,
                    row.Identity,
                    responseReference428,
                    operation,
                    true
                );
            try
            {
                Track(operation);
            }
            catch
            {
                accepted.TrySetCanceled();
                throw;
            }
            row.Commands = operation;
            accepted.TrySetResult();
        }

        return responseReference428 is null ? new(operation.WaitAsync(caller.Cancellation))
            : ValueTask.FromResult<Term>(responseReference428);
    }

    internal ValueTask<Term> CloseOrdered(Term identity, DriverParentPeerTransport422 transport,
        CancellationToken cancellation)
    {
        Task<Term> operation;
        lock (gate)
        {
            var row = Require(identity);
            var accepted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            operation = After(
                row.Commands,
                accepted.Task,
                async () =>
            {
                await Close(row, transport).ConfigureAwait(false);

                return Term.A(ErlangBooleanAtoms.True);
            }
            );
            try
            {
                Track(operation);
            }
            catch
            {
                accepted.TrySetCanceled();
                throw;
            }
            row.Commands = operation;
            row.StopRequested = true;
            row.Writable.TrySetResult();
            accepted.TrySetResult();
        }

        return new(operation.WaitAsync(cancellation));
    }
}
