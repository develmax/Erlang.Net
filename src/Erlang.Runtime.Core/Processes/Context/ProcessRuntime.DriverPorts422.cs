namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal async ValueTask<Term> OpenConfiguredPort422(ProcessContext owner, Term specification, Term settings)
    {
        RuntimeDriverFrontend422? frontend;
        lock (gate) frontend = driverFrontend422;
        if (frontend is not null)
        {
            Term? result = await frontend.TryOpen(owner, specification, settings).ConfigureAwait(false);
            if (result is not null) return result;
        }

        return Ports.Open(owner, specification, settings);
    }

    internal RuntimeDriverFrontend422? DriverFrontendForPort422(Term identity)
    {
        lock (gate)
            return driverFrontend422?.ContainsPort(identity) == true ? driverFrontend422 : null;
    }

    internal PortTerm[] DriverPortSnapshot422()
    {
        lock (gate) return driverFrontend422?.PortSnapshot() ?? [];
    }

    internal ValueTask<Term> DriverControl422(
        ProcessContext caller,
        Term identity,
        Term command,
        Term data,
        bool internalProtocol428 = false
    )
    {
        var frontend = DriverFrontendForPort422(identity)
            ?? throw new ErlangException(ErlangErrorReasons.BadArgument);
        RequireDriverParentActor422(caller);
        if (command is not Integer number || number.Value < uint.MinValue || number.Value > uint.MaxValue)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return frontend.Control(
            caller,
            identity,
            (uint)number.Value,
            ErlangIoData.Flatten(data, false, false).ToArray(),
            internalProtocol428 ? MakeReference() : null
        );
    }

    internal ValueTask<Term> DriverCall422(
        ProcessContext caller,
        Term identity,
        Term command,
        Term data,
        bool internalProtocol428 = false
    )
    {
        var frontend = DriverFrontendForPort422(identity)
            ?? throw new ErlangException(ErlangErrorReasons.BadArgument);
        RequireDriverParentActor422(caller);
        if (command is not Integer number || number.Value < uint.MinValue || number.Value > uint.MaxValue)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        byte[] encoded;
        lock (gate)
        {
            LocalNodeSnapshot wire = LocalIdentity.Snapshot;
            encoded = ExternalTermFormat.Encode(
                data,
                new EtfEncodingOptions
                {
                    LocalNode = wire.Node,
                    LocalCreation = wire.Creation,
                    NodeIdentity = LocalIdentity,
                    NodeSnapshot = wire,
                    LocalSeed = LocalTermSeed,
                    FunctionOwner = caller.Self
                }
            );
        }

        return frontend.Call(
            caller,
            identity,
            (uint)number.Value,
            encoded,
            internalProtocol428 ? MakeReference() : null
        );
    }
}
