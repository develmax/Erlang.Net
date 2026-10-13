using System.ComponentModel;
using System.Diagnostics;

namespace Erlang;

internal sealed class RuntimePorts(ProcessRuntime runtime, object gate)
{
    private readonly Dictionary<PortTerm, RuntimePort> ports = new();
    private readonly List<Task> pumps = new();
    private ulong nextId;

    public Term Open(ProcessContext owner, Term specification, Term settings)
    {
        PortOpenOptions options = PortOpenOptions.Parse(specification, settings);
        var process = new Process { StartInfo = options.Start };
        try
        {
            process.Start();
        }
        catch (Win32Exception exception)
        {
            process.Dispose();
            throw new ErlangException(exception.NativeErrorCode == PortParameters.FileNotFoundError || OperatingSystem.IsWindows() && exception.NativeErrorCode == PortParameters.WindowsPathNotFoundError ? PortNames.NoEntry : PortNames.AccessDenied);
        }
        lock (gate)
        {
            if (owner.Terminated)
            {
                process.StandardInput.Close();
                process.StandardOutput.Close();
                process.Dispose();
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            var identity = new PortTerm(runtime.Node, ++nextId);
            var port = new RuntimePort(
                identity,
                owner.Self,
                process,
                options
            );
            ports.Add(identity, port);
            owner.PortLinks.Add(identity);
            port.Pump = Task.Run(() => Pump(port));
            pumps.Add(port.Pump);

            return identity;
        }
    }

    public RuntimePort? Find(Term identity)
    {
        if (identity is Atom name)
            identity = runtime.WhereIs(name.Name);

        return identity is PortTerm port ? ports.GetValueOrDefault(port) : null;
    }

    private RuntimePort Require(Term identity) => Find(identity) ?? throw new ErlangException(ErlangErrorReasons.BadArgument);

    public bool Alive(PortTerm identity) => ports.ContainsKey(identity);

    public Term Snapshot()
    {
        lock (gate)
            return Cons.From(ports.Keys);
    }

    public Term GetData(Term identity)
    {
        lock (gate)
            return Require(identity).Data;
    }

    public Term SetData(Term identity, Term data)
    {
        lock (gate)
            Require(identity).Data = data;

        return Term.A(ErlangBooleanAtoms.True);
    }

    public Term Close(Term identity)
    {
        lock (gate)
            Close(Require(identity), Term.A(ProcessExitReasons.Normal));

        return Term.A(ErlangBooleanAtoms.True);
    }

    public async ValueTask<Term> CloseOrdered(Term identity)
    {
        await AwaitCommands(identity);

        return Close(identity);
    }

    public async ValueTask AwaitCommands(Term identity)
    {
        Task previous;
        lock (gate)
            previous = Find(identity)?.Commands ?? Task.CompletedTask;
        try
        {
            await previous;
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
        // Resource disposal can block in the OS; it must not hold the runtime gate.
        Task cleanup = Task.Run(async () =>
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
            if (port.Pump is not null)
                await port.Pump;
            port.Process.Dispose();
        });
        pumps.Add(cleanup);
    }

    public Term Connect(Term identity, Term destination)
    {
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
            PortNames.Links => Cons.From(port.Links),
            PortNames.MonitoredBy => runtime.PortMonitoredBy(port.Identity),
            PortNames.Monitors => Nil.Value,
            PortNames.Id => new Integer(port.Identity.Id),
            PortNames.Connected => port.Connected,
            PortNames.Input => Term.I(port.InputBytes),
            PortNames.Output => Term.I(port.OutputBytes),
            PortNames.OsPid => Term.I(port.OsPid),
            PortNames.RegisteredName => runtime.PortRegisteredName(port.Identity) is string name ? Term.A(name) : Nil.Value,
            _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
        };

        return Term.Tuple(key, value);
    }

    public async ValueTask<Term> Command(Term identity, Term data, Term flags)
    {
        await AwaitCommands(identity);

        return await CommandCore(identity, data, flags);
    }

    private async ValueTask<Term> CommandCore(Term identity, Term data, Term flags)
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
        RuntimePort port;
        lock (gate)
            port = Require(identity);
        if (force)
            throw new ErlangException(PortNames.NotSupported);
        byte[] bytes = ErlangIoData.Flatten(data, false, false).ToArray();
        bool entered;
        try
        {
            entered = noSuspend ? port.WriteGate.Wait(0) : await port.WriteGate.WaitAsync(Timeout.Infinite, port.Stop.Token);
        }
        catch (OperationCanceledException)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        if (!entered)
            return Term.A(ErlangBooleanAtoms.False);
        try
        {
            int width = port.Options.PacketBytes;
            if (width != 0)
            {
                if ((ulong)bytes.Length >= (1UL << (width * PortParameters.ByteBits)))
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                var header = new byte[width];
                uint length = (uint)bytes.Length;
                for (int index = width - 1; index >= 0; index--)
                {
                    header[index] = (byte)length;
                    length >>= PortParameters.ByteBits;
                }
                await port.Input.WriteAsync(header, port.Stop.Token);
            }
            await port.Input.WriteAsync(bytes, port.Stop.Token);
            await port.Input.FlushAsync(port.Stop.Token);
            lock (gate)
                port.OutputBytes += bytes.Length;

            return Term.A(ErlangBooleanAtoms.True);
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException or OperationCanceledException)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        finally
        {
            port.WriteGate.Release();
        }
    }

    private async Task Pump(RuntimePort port)
    {
        try
        {
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
                        runtime.Send(port.Connected, Term.Tuple(port.Identity, Term.A(PortNames.Eof)));
                }
            await port.Process.WaitForExitAsync(port.Stop.Token);
            lock (gate)
            {
                if (port.Closed)
                    return;
                if (port.Options.ExitStatus)
                    runtime.Send(port.Connected, Term.Tuple(port.Identity, Term.Tuple(Term.A(PortNames.ExitStatus), Term.I(port.Process.ExitCode))));
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
    }

    public void Shutdown()
    {
        foreach (RuntimePort port in ports.Values.ToArray())
            Close(port, Term.A(ProcessExitReasons.Shutdown));
    }

    public void SendExit(ProcessContext sender, PortTerm identity, Term reason)
    {
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
        runtime.Send(port.Connected, Term.Tuple(port.Identity, Term.Tuple(Term.A(PortNames.Data), data)));
    }

    public Term Send(Term identity, Term message)
    {
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
                    lock (gate)
                    {
                        Close(port, Term.A(ProcessExitReasons.Normal));
                        runtime.Send(connected, Term.Tuple(port.Identity, Term.A(PortNames.Closed)));
                    }
                });
                pumps.Add(port.Commands);
            }
            else if (operation is TupleTerm { Items.Count: 2 } command && command.Items[0] is Atom { Name: PortNames.ProtocolCommand })
            {
                Task previous = port.Commands;
                port.Commands = Task.Run(async () =>
                {
                    await previous;
                    try
                    {
                        await CommandCore(port.Identity, command.Items[1], Nil.Value);
                    }
                    catch (ErlangException)
                    {
                        lock (gate)
                            if (!port.Closed)
                                runtime.PortBadSignal(port);
                    }
                });
                pumps.Add(port.Commands);
            }
            else if (operation is TupleTerm { Items.Count: 2 } connect && connect.Items[0] is Atom { Name: PortNames.ProtocolConnect } && connect.Items[1] is Pid destination)
            {
                Pid previous = port.Connected;
                port.Connected = destination;
                runtime.Send(previous, Term.Tuple(port.Identity, Term.A(PortNames.Connected)));
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
