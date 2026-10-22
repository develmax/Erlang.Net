namespace Erlang;

internal sealed partial class RuntimeDriverFrontend422
{
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

    internal void LinkPort(ProcessContext owner, PortTerm identity, bool priority) => ports.Link(owner, identity, priority);

    internal void UnlinkPort(ProcessContext owner, PortTerm identity) => ports.Unlink(owner, identity);

    internal void QueueClosePort(Term identity) => ports.QueueCloseSignal(identity, Term.A(ProcessExitReasons.Normal), transport);

    internal void QueuePortExit(Term identity, Term reason) => ports.QueueCloseSignal(identity, reason, transport);

    internal async ValueTask AwaitPortCommands(Term identity, CancellationToken cancellation)
        => await ports.AwaitCommands(identity).WaitAsync(cancellation).ConfigureAwait(false);

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

    internal ValueTask<Term> ClosePort(Term identity, CancellationToken cancellation)
        => ports.CloseOrdered(identity, transport, cancellation);

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
