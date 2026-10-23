namespace Erlang;

internal sealed partial class RuntimeDriverFrontend422
{
    internal ValueTask<Term> ConnectPort455(ProcessContext caller, Term identity, Term destination)
    {
        runtime.RequireDriverParentActor422(caller);
        RequireReady();

        return ports.ConnectOrdered455(
            caller,
            identity,
            destination,
            transport
        );
    }

    internal async ValueTask<Term?> TryOpen(ProcessContext context, Term specification, Term settings)
    {
        runtime.RequireDriverParentActor422(context);
        if (specification is not TupleTerm { Items.Count: 2 } tuple
            || tuple.Items[0] is not Atom { Name: PortNames.Spawn or DriverOpenNames422.SpawnDriver } kind) return null;
        RequireReady();
        byte[] command = DriverOpenCommand422.Read(tuple.Items[1], context.Cancellation);
        ErlDriverName420 name = DriverOpenCommand422.Name(command);
        if (!await drivers.CanOpen(context, name).ConfigureAwait(false))
        {
            if (kind.Name == DriverOpenNames422.SpawnDriver)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return null;
        }
        // Reuse the existing option parser, not its OS child-launch backend.
        // The unused ProcessStartInfo is merely parser output; no fake process
        // is constructed or started. Native command bytes remain authoritative.
        foreach (Term setting in Cons.Items(settings))
            if (setting is TupleTerm { Items.Count: 2 } pair
                && pair.Items[0] is Atom { Name: PortNames.BusyLimitsPort })
                throw new ErlangException(ErlangErrorReasons.BadArgument);
        PortOpenOptions options = PortOpenOptions.Parse(
            Term.Tuple(Term.A(PortNames.Spawn), tuple.Items[1]),
            settings,
            runtime.RequireOsEnvironment(),
            context.Cancellation
        );
        if (options.ExitStatus) throw new ErlangException(ErlangErrorReasons.BadArgument);

        return await ports.Open(
            context,
            name,
            command,
            options,
            transport,
            configuration
        ).ConfigureAwait(false);
    }

    internal bool ContainsPort(Term identity) => ports.Contains(identity);

    internal PortTerm[] PortSnapshot() => ports.Snapshot();

    internal Term GetPortData(Term identity) => ports.GetData(identity);

    internal Term SetPortData(Term identity, Term data) => ports.SetData(identity, data);

    internal Term PortInfo(Term identity, Term? item) => ports.Info(identity, item);

    internal ValueTask<Term> PortInfoQueued454(ProcessContext caller, Term identity, Term? item)
        => ports.InfoQueued454(caller, identity, item);

    internal void LinkPort(ProcessContext owner, PortTerm identity, bool priority) => ports.Link(owner, identity, priority);

    internal void UnlinkPort(ProcessContext owner, PortTerm identity) => ports.Unlink(owner, identity);

    internal void QueueClosePort(Term identity) => ports.QueueCloseSignal(identity, Term.A(ProcessExitReasons.Normal), transport);

    internal void QueuePortExit(Term identity, Term reason, Pid sender) => ports.QueueCloseSignal(
        identity,
        reason,
        transport,
        sender
    );

    internal async ValueTask AwaitPortCommands(Term identity, CancellationToken cancellation, ProcessContext? caller = null)
        => await ports.AwaitCommands(identity, caller).WaitAsync(cancellation).ConfigureAwait(false);

    internal ValueTask<NativeDriverRawSendResult454> SendRaw(
        ProcessContext caller,
        Term identity,
        Term message,
        bool noSuspend,
        CancellationToken cancellation
    )
    {
        RequireReady();

        return ports.SendRaw(
            caller,
            identity,
            message,
            noSuspend,
            cancellation,
            transport
        );
    }

    internal ValueTask<Term> Command(
        ProcessContext caller,
        Term identity,
        Term data,
        bool noSuspend,
        bool force
    )
    {
        RequireReady();

        return ports.Command(
            caller,
            identity,
            data,
            noSuspend,
            force,
            transport
        );
    }

    internal ValueTask<Term> ClosePort(Term identity, CancellationToken cancellation, ProcessContext? caller = null)
        => ports.CloseOrdered(
            identity,
            transport,
            cancellation,
            caller
        );

    internal ValueTask<Term> Control(
        ProcessContext caller,
        Term identity,
        uint command,
        byte[] data,
        ReferenceTerm? responseReference428 = null
    )
    {
        RequireReady();

        return ports.Control(
            caller,
            identity,
            command,
            data,
            transport,
            responseReference428
        );
    }

    internal ValueTask<Term> Call(
        ProcessContext caller,
        Term identity,
        uint command,
        byte[] data,
        ReferenceTerm? responseReference428 = null
    )
    {
        RequireReady();

        return ports.Call(
            caller,
            identity,
            command,
            data,
            transport,
            responseReference428
        );
    }
}
