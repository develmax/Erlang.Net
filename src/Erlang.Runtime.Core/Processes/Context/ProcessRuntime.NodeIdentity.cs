namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal Pid RehydratePid(Pid pid)
    {
        return RehydratePid(pid, LocalIdentity.Snapshot);
    }

    internal Pid RehydratePid(Pid pid, LocalNodeSnapshot wire)
    {
        return pid.Node == wire.Node && pid.Creation == wire.Creation ? new Pid(LocalIdentity, pid.Id) : pid;
    }

    internal PortTerm RehydratePort(PortTerm port)
    {
        return RehydratePort(port, LocalIdentity.Snapshot);
    }

    internal PortTerm RehydratePort(PortTerm port, LocalNodeSnapshot wire)
    {
        return port.Node == wire.Node && port.Creation == wire.Creation ? new PortTerm(LocalIdentity, port.Id) : port;
    }

    internal uint SelectObservedExternalEpochCreation(string node, uint requested)
    {
        uint creation = Math.Max(requested, RuntimeNodeIdentityParameters.MinimumActiveCreation);
        uint initial = creation;
        while (KnownNodes.HasRetainedExternalEpoch(node, creation))
        {
            creation = unchecked(creation + 1);
            if (creation < RuntimeNodeIdentityParameters.MinimumActiveCreation)
                creation = RuntimeNodeIdentityParameters.MinimumActiveCreation;
            if (creation == initial)
                throw new InvalidOperationException(RuntimeNodeIdentityDiagnostics.CreationCapacity);
        }

        return creation;
    }
}
