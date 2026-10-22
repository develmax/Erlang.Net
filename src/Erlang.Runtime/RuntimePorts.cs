using System.ComponentModel;
using System.Diagnostics;

namespace Erlang;

internal sealed partial class RuntimePorts(ProcessRuntime runtime, object gate)
{
    private readonly Dictionary<PortTerm, RuntimePort> ports = new();
    private readonly HashSet<Task> pumps = new();
    private ulong nextId;

    public Term Open(ProcessContext owner, Term specification, Term settings)
    {
        runtime.ThrowIfHalted();
        PortOpenOptions options = PortOpenOptions.Parse(
            specification,
            settings,
            runtime.OsEnvironment,
            owner.Cancellation
        );
        var operation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (gate)
        {
            runtime.ThrowIfHalted();
            Track(operation.Task);
        }
        Task? rejectedCleanup = null;
        try
        {
            PortChildProcess process;
            try
            {
                process = PortChildProcess.Start(options);
            }
            catch (Win32Exception exception)
            {
                throw new ErlangException(exception.NativeErrorCode == PortParameters.FileNotFoundError || OperatingSystem.IsWindows() && exception.NativeErrorCode == PortParameters.WindowsPathNotFoundError ? PortNames.NoEntry : PortNames.AccessDenied);
            }
            lock (gate)
            {
                if (owner.Terminated || runtime.IsHaltRequested)
                {
                    rejectedCleanup = Task.Run(async () =>
                    {
                        process.Input.Close();
                        process.Output.Close();
                        if (process.HasChild)
                            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
                        process.Dispose();
                    });
                    Track(rejectedCleanup);
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                }
                var identity = new PortTerm(runtime.LocalIdentity, ++nextId);
                var port = new RuntimePort(
                    identity,
                    owner.Self,
                    process,
                    options
                );
                ports.Add(identity, port);
                owner.PortLinks.Add(identity);
                port.Pump = Task.Run(() => Pump(port));
                Track(port.Pump);

                return identity;
            }
        }
        finally
        {
            if (rejectedCleanup is null)
                operation.TrySetResult();
            else
                _ = FinishOpening(operation, rejectedCleanup);
        }
    }

    public RuntimePort? Find(Term identity)
    {
        if (identity is Atom name)
            identity = runtime.WhereIs(name.Name);

        return identity is PortTerm port ? ports.GetValueOrDefault(port) : null;
    }

    private RuntimePort Require(Term identity) => Find(identity) ?? throw new ErlangException(ErlangErrorReasons.BadArgument);

    public bool Alive(PortTerm identity) => ports.ContainsKey(identity) || runtime.DriverFrontendForPort422(identity) is not null;

    public Term Snapshot()
    {
        lock (gate)
        {
            PortTerm[] native = runtime.DriverPortSnapshot422();

            return native.Length == 0 ? Cons.From(ports.Keys) : Cons.From(ports.Keys.Concat(native));
        }
    }

    public Term GetData(Term identity)
    {
        lock (gate)
            return runtime.DriverFrontendForPort422(identity) is { } native ? native.GetPortData(identity) : Require(identity).Data;
    }

    public Term SetData(Term identity, Term data)
    {
        lock (gate)
            if (runtime.DriverFrontendForPort422(identity) is { } native) native.SetPortData(identity, data);
            else Require(identity).Data = data;

        return Term.A(ErlangBooleanAtoms.True);
    }

    public Term Close(Term identity)
    {
        lock (gate)
            if (runtime.DriverFrontendForPort422(identity) is { } native) native.QueueClosePort(identity);
            else Close(Require(identity), Term.A(ProcessExitReasons.Normal));

        return Term.A(ErlangBooleanAtoms.True);
    }

    public ValueTask<Term> CloseOrdered(Term identity, CancellationToken cancellation = default)
    {
        if (runtime.DriverFrontendForPort422(identity) is { } native) return native.ClosePort(identity, cancellation);
        Task<Term> operation;
        lock (gate)
        {
            RuntimePort port = Require(identity);
            operation = CloseOrderedCore(port, port.Commands);
            port.Commands = operation;
            Track(operation);
        }

        return new ValueTask<Term>(operation.WaitAsync(cancellation));
    }

    private async Task<Term> CloseOrderedCore(RuntimePort port, Task previous)
    {
        try
        {
            await previous.ConfigureAwait(false);
        }
        catch (ErlangException)
        {
            // An earlier failed command must not poison an accepted close operation.
        }
        Task writes;
        lock (gate)
            writes = port.Writes;
        await writes.ConfigureAwait(false);

        return Close(port.Identity);
    }

    public async ValueTask AwaitCommands(Term identity, CancellationToken cancellation = default)
    {
        if (runtime.DriverFrontendForPort422(identity) is { } native)
        {
            await native.AwaitPortCommands(identity, cancellation);

            return;
        }
        Task previous;
        lock (gate)
            previous = Find(identity)?.Commands ?? Task.CompletedTask;
        try
        {
            await previous.WaitAsync(cancellation);
        }
        catch (ErlangException)
        {
            // An unsuccessful earlier command does not poison subsequent signals.
        }
    }

    private void Close(RuntimePort port, Term reason)
    {
        if (!ports.Remove(port.Identity))
            return;
        port.Closed = true;
        runtime.PortRemoved(port.Identity, port.Links.ToArray(), reason);
        port.Links.Clear();
        port.Stop.Cancel();
        port.Writable.TrySetResult();
        port.MessageWritable.TrySetResult();
        // Resource disposal can block in the OS; it must not hold the runtime gate.
        port.Cleanup = Task.Run(async () =>
        {
            try
            {
                port.Input.Close();
            }
            catch (IOException) { }
            try
            {
                port.Output.Close();
            }
            catch (IOException) { }
            try
            {
                if (port.HaltOwnedLifetime)
                    await Task.WhenAll(
                        port.Pump ?? Task.CompletedTask,
                        port.Writes,
                        port.Commands,
                        port.HaltChildExit
                    );
                else
                {
                    if (port.Pump is not null)
                        await port.Pump;
                    await port.Writes;
                }
            }
            finally
            {
                port.Process.Dispose();
            }
        });
        Track(port.Cleanup);
    }

    public Term Connect(Term identity, Term destination)
    {
        if (runtime.DriverFrontendForPort422(identity) is not null)
        {
            if (destination is not Pid target || !runtime.DriverTargetAlive422(target)) throw new ErlangException(ErlangErrorReasons.BadArgument);
            throw new NotSupportedException(NativeDriverPortNames422.ConnectUnsupported);
        }
        lock (gate)
        {
            RuntimePort port = Require(identity);
            if (destination is not Pid pid || !runtime.AddPortLink(pid, port.Identity))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            port.Links.Add(pid);
            port.Connected = pid;
        }

        return Term.A(ErlangBooleanAtoms.True);
    }

    public void Link(ProcessContext owner, PortTerm identity, bool priority = false)
    {
        if (runtime.DriverFrontendForPort422(identity) is { } native)
        {
            native.LinkPort(owner, identity, priority);

            return;
        }
        lock (gate)
        {
            if (identity.Node != runtime.Node)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            RuntimePort? port = Find(identity);
            if (port is null && !owner.TrapExits)
                throw new ErlangException(ErlangErrorReasons.NoProcess);
            if (priority)
                owner.PriorityPortLinks.Add(identity);
            else
                owner.PriorityPortLinks.Remove(identity);
            if (port is null)
            {
                owner.PortLinks.Add(identity);
                runtime.PortRemoved(identity, new[] { owner.Self }, Term.A(ErlangErrorReasons.NoProcess));

                return;
            }
            port.Links.Add(owner.Self);
            owner.PortLinks.Add(identity);
        }
    }

    public void Unlink(ProcessContext owner, PortTerm identity)
    {
        if (runtime.DriverFrontendForPort422(identity) is { } native)
        {
            native.UnlinkPort(owner, identity);

            return;
        }
        lock (gate)
        {
            if (identity.Node != runtime.Node)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            owner.PortLinks.Remove(identity);
            owner.PriorityPortLinks.Remove(identity);
            Find(identity)?.Links.Remove(owner.Self);
        }
    }

    public void OwnerTerminated(ProcessContext owner, Term reason)
    {
        foreach (PortTerm identity in owner.PortLinks.ToArray())
        {
            RuntimePort? port = Find(identity);
            if (port is null)
                continue;
            port.Links.Remove(owner.Self);
            if (port.Connected.Equals(owner.Self) || !reason.Equals(Term.A(ProcessExitReasons.Normal)))
                Close(port, reason);
        }
        owner.PortLinks.Clear();
        owner.PriorityPortLinks.Clear();
    }

    public Term Info(Term identity, Term? item)
    {
        if (runtime.DriverFrontendForPort422(identity) is { } native) return native.PortInfo(identity, item);
        lock (gate)
        {
            if (identity is not Atom && (identity is not PortTerm foreign || foreign.Node != runtime.Node))
                throw new ErlangException(PortNames.BadType);
            RuntimePort? port = Find(identity);
            if (port is null)
                return Term.A(ProcessRegistryAtoms.Undefined);
            if (item is not null)
                return InfoItem(port, item);
            var result = new List<Term>();
            string? registered = runtime.PortRegisteredName(port.Identity);
            if (registered is not null)
                result.Add(Term.Tuple(Term.A(PortNames.RegisteredName), Term.A(registered)));
            foreach (string key in new[] { PortNames.Name, PortNames.Links, PortNames.Id, PortNames.Connected, PortNames.Input, PortNames.Output, PortNames.OsPid })
                result.Add(InfoItem(port, Term.A(key)));

            return Cons.From(result);
        }
    }

    private Term InfoItem(RuntimePort port, Term item)
    {
        if (item is not Atom key)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (key.Name == PortNames.RegisteredName && runtime.PortRegisteredName(port.Identity) is null)
            return Nil.Value;
        Term value = key.Name switch
        {
            PortNames.Name => Cons.From(port.Options.Name.Select(character => Term.I(character))),
            PortNames.QueueSize => new Integer(port.QueuedBytes),
            PortNames.Links => Cons.From(port.Links),
            PortNames.MonitoredBy => runtime.PortMonitoredBy(port.Identity),
            PortNames.Monitors => Nil.Value,
            PortNames.Id => new Integer(port.Identity.Id),
            PortNames.Connected => port.Connected,
            PortNames.Input => Term.I(port.InputBytes),
            PortNames.Output => Term.I(port.OutputBytes),
            PortNames.OsPid => port.Process.HasChild ? Term.I(port.OsPid) : Term.A(ProcessRegistryAtoms.Undefined),
            PortNames.RegisteredName => runtime.PortRegisteredName(port.Identity) is string name ? Term.A(name) : Nil.Value,
            _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
        };

        return Term.Tuple(key, value);
    }

    public async ValueTask<Term> Command(
        Term identity,
        Term data,
        Term flags,
        ProcessContext? caller = null
    )
    {
        var nativeFlags = ParseCommandFlags(flags);
        if (runtime.DriverFrontendForPort422(identity) is { } native)
        {
            if (caller is null) throw new NotSupportedException(NativeDriverPortNames422.CallerRequired);

            return await native.Command(
                caller,
                identity,
                data,
                nativeFlags.NoSuspend,
                nativeFlags.Force
            );
        }
        await AwaitCommands(identity, caller?.Cancellation ?? CancellationToken.None);

        return await CommandCore(
            identity,
            data,
            flags,
            caller: caller
        );
    }

    private static (bool NoSuspend, bool Force) ParseCommandFlags(Term flags)
    {
        bool noSuspend = false;
        bool force = false;
        Term[] flagItems;
        try
        {
            flagItems = Cons.Items(flags).ToArray();
        }
        catch (ErlangException)
        {
            throw new ErlangException(PortNames.BadOption);
        }
        foreach (Term flag in flagItems)
        {
            if (flag is Atom { Name: PortNames.Force })
            {
                force = true;
                continue;
            }
            if (flag is not Atom { Name: PortNames.NoSuspend })
                throw new ErlangException(PortNames.BadOption);
            noSuspend = true;
        }

        return (noSuspend, force);
    }

    private async ValueTask<Term> CommandCore(
        Term identity,
        Term data,
        Term flags,
        bool accepted = false,
        ProcessContext? caller = null
    )
    {
        if (!accepted)
            runtime.ThrowIfHalted();
        (bool noSuspend, bool force) = ParseCommandFlags(flags);
        RuntimePort port;
        lock (gate)
            port = Require(identity);
        if (force)
            throw new ErlangException(PortNames.NotSupported);
        byte[] bytes = ErlangIoData.Flatten(data, false, false).ToArray();
        while (true)
        {
            Task writable;
            lock (gate)
            {
                if (!accepted)
                    runtime.ThrowIfHalted();
                if (port.Closed)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                if (!port.Busy)
                {
                    int width = port.Options.PacketBytes;
                    port.OutputBytes += bytes.Length;
                    if (width != 0 && (ulong)bytes.Length >= (1UL << (width * PortParameters.ByteBits)))
                    {
                        Close(port, Term.A(PortNames.InvalidValue));

                        return Term.A(ErlangBooleanAtoms.True);
                    }
                    var frame = new byte[checked(bytes.Length + width)];
                    uint length = (uint)bytes.Length;
                    for (int index = width - 1; index >= 0; index--)
                    {
                        frame[index] = (byte)length;
                        length >>= PortParameters.ByteBits;
                    }
                    bytes.CopyTo(frame, width);
                    port.QueuedBytes += (ulong)frame.Length;
                    if (port.QueuedBytes >= port.Options.HighWatermark)
                        port.Busy = true;
                    Task previous = port.Writes;
                    port.Writes = port.Options.Parallelism
                        ? Task.Run(() => Write(port, previous, frame))
                        : Write(port, previous, frame);
                    Track(port.Writes);

                    return Term.A(ErlangBooleanAtoms.True);
                }
                if (noSuspend)
                    return Term.A(ErlangBooleanAtoms.False);
                writable = port.Writable.Task;
                if (caller is not null && !writable.IsCompleted)
                {
                    caller.Cancellation.ThrowIfCancellationRequested();
                    runtime.SystemMonitoring.BusyPort(caller.Self, port.Identity);
                }
            }
            await writable.WaitAsync(caller?.Cancellation ?? CancellationToken.None);
        }
    }

    private async Task Write(RuntimePort port, Task previous, byte[] frame)
    {
        try
        {
            await previous.ConfigureAwait(false);
            if (!port.Options.Write && (frame.Length != 0 || OperatingSystem.IsLinux()))
                throw new NativePortIOException(NativePortIoParameters.BadDescriptor);
            await port.Input.WriteAsync(frame, port.Stop.Token).ConfigureAwait(false);
            await port.Input.FlushAsync(port.Stop.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException or OperationCanceledException)
        {
            lock (gate)
                if (!port.Closed)
                    Close(port, Term.A(NativePortIoErrors.Reason(exception)));
        }
        finally
        {
            lock (gate)
            {
                port.QueuedBytes -= (ulong)frame.Length;
                if (port.Busy && (port.QueuedBytes == 0 || port.QueuedBytes < port.Options.LowWatermark))
                {
                    port.Busy = false;
                    TaskCompletionSource ready = port.Writable;
                    port.Writable = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    ready.TrySetResult();
                }
            }
        }
    }

    private void Track(Task task)
    {
        if (task.IsCompleted)
            return;
        pumps.Add(task);
        _ = task.ContinueWith(
            completed =>
        {
            lock (gate)
                pumps.Remove(completed);
        },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default
        );
    }

    private async Task Pump(RuntimePort port)
    {
        try
        {
            if (!port.Options.Read)
            {
                if (!port.Process.HasChild || !OperatingSystem.IsWindows() || !port.Options.ExitStatus)
                    await Task.Delay(Timeout.Infinite, port.Stop.Token);
                else
                {
                    await port.Process.WaitForExitAsync(port.Stop.Token);
                    lock (gate)
                    {
                        if (port.Closed)
                            return;
                        runtime.SendPortNotification(port.Connected, Term.Tuple(port.Identity, Term.Tuple(Term.A(PortNames.ExitStatus), Term.I(port.Process.ExitCode))));
                        if (port.Options.Eof)
                            runtime.SendPortNotification(port.Connected, Term.Tuple(port.Identity, Term.A(PortNames.Eof)));
                        else
                            Close(port, Term.A(ProcessExitReasons.Normal));
                    }
                }

                return;
            }
            Stream stream = port.Output;
            byte[] buffer = new byte[PortParameters.ReadBufferBytes];
            while (true)
            {
                byte[] payload;
                int width = port.Options.PacketBytes;
                if (width == 0)
                {
                    int count = await stream.ReadAsync(buffer, port.Stop.Token);
                    if (count == 0)
                        break;
                    payload = buffer.AsSpan(0, count).ToArray();
                }
                else
                {
                    var header = new byte[width];
                    int first = await stream.ReadAsync(header.AsMemory(0, 1), port.Stop.Token);
                    if (first == 0)
                        break;
                    await stream.ReadExactlyAsync(header.AsMemory(1), port.Stop.Token);
                    uint length = 0;
                    foreach (byte octet in header)
                        length = (length << PortParameters.ByteBits) | octet;
                    if (length > PortParameters.MaximumPacketBytes)
                    {
                        lock (gate)
                            Close(port, Term.A(ErlangErrorReasons.SystemLimit));

                        return;
                    }
                    payload = new byte[checked((int)length)];
                    await stream.ReadExactlyAsync(payload, port.Stop.Token);
                }
                lock (gate)
                {
                    if (port.Closed)
                        return;
                    port.InputBytes += payload.Length;
                    if (port.Lines is null)
                        Deliver(port, payload, null);
                    else
                        foreach (PortLineResult line in port.Lines.Read(payload))
                            Deliver(port, line.Data, line.EndOfLine);
                }
            }
            lock (gate)
                if (!port.Closed)
                {
                    FlushLines(port);
                    if (port.Options.Eof)
                        runtime.SendPortNotification(port.Connected, Term.Tuple(port.Identity, Term.A(PortNames.Eof)));
                }
            await port.Process.WaitForExitAsync(port.Stop.Token);
            lock (gate)
            {
                if (port.Closed)
                    return;
                if (port.Process.HasChild && port.Options.ExitStatus)
                    runtime.SendPortNotification(port.Connected, Term.Tuple(port.Identity, Term.Tuple(Term.A(PortNames.ExitStatus), Term.I(port.Process.ExitCode))));
                if (!port.Options.Eof)
                    Close(port, Term.A(ProcessExitReasons.Normal));
            }
        }
        catch (Exception exception) when (exception is IOException or OperationCanceledException or ObjectDisposedException or OverflowException or InvalidOperationException)
        {
            lock (gate)
                if (!port.Closed)
                    Close(port, Term.A(PortNames.IoError));
        }
        catch (NotSupportedException) when (port.Stop.IsCancellationRequested && port.Closed)
        {
            // FileStream can reject a disposed reader before checking cancellation.
            // Only an already canceled, closed port has completed this teardown.
        }
    }

    public void Shutdown()
    {
        foreach (RuntimePort port in ports.Values.ToArray())
            Close(port, Term.A(ProcessExitReasons.Shutdown));
    }

    public void SendExit(ProcessContext sender, PortTerm identity, Term reason)
    {
        if (runtime.DriverFrontendForPort422(identity) is { } native)
        {
            native.QueuePortExit(identity, reason.Equals(Term.A(ProcessExitSignals.Kill)) ? Term.A(ProcessExitReasons.Killed) : reason);

            return;
        }
        lock (gate)
        {
            if (identity.Node != runtime.Node)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            RuntimePort? port = Find(identity);
            if (port is not null)
                Close(port, reason.Equals(Term.A(ProcessExitSignals.Kill)) ? Term.A(ProcessExitReasons.Killed) : reason);
        }
    }

    private void FlushLines(RuntimePort port)
    {
        if (port.Lines is null)
            return;
        foreach (PortLineResult line in port.Lines.Flush())
            Deliver(port, line.Data, line.EndOfLine);
    }

    private void Deliver(RuntimePort port, byte[] payload, bool? endOfLine)
    {
        Term data = port.Options.Binary ? new BitString(payload) : Cons.From(payload.Select(octet => Term.I(octet)));
        if (endOfLine is bool eol)
            data = Term.Tuple(Term.A(eol ? PortLineNames.EndOfLine : PortLineNames.NoEndOfLine), data);
        runtime.SendPortNotification(port.Connected, Term.Tuple(port.Identity, Term.Tuple(Term.A(PortNames.Data), data)));
    }

    public async ValueTask<bool> SendAsync(
        Term identity,
        Term message,
        bool noSuspend,
        CancellationToken cancellation,
        ProcessContext? caller = null
    )
    {
        if (runtime.DriverFrontendForPort422(identity) is not null) throw new NotSupportedException(NativeDriverPortNames422.RawMessageUnsupported);
        while (true)
        {
            Task? wait;
            bool accepted = false;
            lock (gate)
            {
                RuntimePort? port = Find(identity);
                if (port is null)
                    return true;
                wait = port.Busy ? port.Writable.Task : port.MessagesBusy ? port.MessageWritable.Task : null;
                if (wait is null)
                {
                    Send(identity, message);
                    accepted = true;
                    if (!noSuspend && port.MessagesBusy)
                        wait = port.MessageWritable.Task;
                    else
                        return true;
                }
                else if (noSuspend)
                    return false;
                if (caller is not null && !wait.IsCompleted)
                {
                    cancellation.ThrowIfCancellationRequested();
                    runtime.SystemMonitoring.BusyPort(caller.Self, port.Identity);
                }
            }
            await wait.WaitAsync(cancellation);
            if (accepted)
                return true;
        }
    }

    public Term Send(Term identity, Term message)
    {
        if (runtime.DriverFrontendForPort422(identity) is not null) throw new NotSupportedException(NativeDriverPortNames422.RawMessageUnsupported);
        lock (gate)
        {
            RuntimePort? port = Find(identity);
            if (port is null)
                return message;
            if (message is not TupleTerm { Items.Count: 2 } envelope || !envelope.Items[0].Equals(port.Connected))
            {
                runtime.PortBadSignal(port);

                return message;
            }
            Term operation = envelope.Items[1];
            if (operation is Atom { Name: PortNames.ProtocolClose })
            {
                Pid connected = port.Connected;
                Task previous = port.Commands;
                port.Commands = Task.Run(async () =>
                {
                    await previous;
                    await port.Writes;
                    lock (gate)
                    {
                        Close(port, Term.A(ProcessExitReasons.Normal));
                        runtime.SendPortNotification(connected, Term.Tuple(port.Identity, Term.A(PortNames.Closed)));
                    }
                });
                Track(port.Commands);
            }
            else if (operation is TupleTerm { Items.Count: 2 } command && command.Items[0] is Atom { Name: PortNames.ProtocolCommand })
            {
                ulong size = 0;
                try
                {
                    size = (ulong)ErlangIoData.Flatten(command.Items[1], false, false).BitLength / PortParameters.ByteBits;
                }
                catch (ErlangException) { }
                port.MessageBytes += size;
                if (port.MessageBytes > port.Options.MessageHighWatermark)
                    port.MessagesBusy = true;
                Task previous = port.Commands;
                port.Commands = Task.Run(async () =>
                {
                    await previous;
                    try
                    {
                        await CommandCore(
                            port.Identity,
                            command.Items[1],
                            Nil.Value,
                            accepted: true
                        );
                    }
                    catch (ErlangException)
                    {
                        lock (gate)
                            if (!port.Closed)
                                runtime.PortBadSignal(port);
                    }
                    finally
                    {
                        lock (gate)
                        {
                            port.MessageBytes -= size;
                            if (port.MessagesBusy && port.MessageBytes < port.Options.MessageLowWatermark)
                            {
                                port.MessagesBusy = false;
                                TaskCompletionSource ready = port.MessageWritable;
                                port.MessageWritable = new(TaskCreationOptions.RunContinuationsAsynchronously);
                                ready.TrySetResult();
                            }
                        }
                    }
                });
                Track(port.Commands);
            }
            else if (operation is TupleTerm { Items.Count: 2 } connect && connect.Items[0] is Atom { Name: PortNames.ProtocolConnect } && connect.Items[1] is Pid destination)
            {
                Pid previous = port.Connected;
                port.Connected = destination;
                runtime.SendPortNotification(previous, Term.Tuple(port.Identity, Term.A(PortNames.Connected)));
            }
            else
                runtime.PortBadSignal(port);

            return message;
        }
    }

    public Task Drain()
    {
        lock (gate)
            return Task.WhenAll(pumps.ToArray());
    }
}
