namespace Erlang;

internal sealed class PreparedExternalIdentifier : IDisposable
{
    internal PhysicalNodeLease Node { get; }
    internal NativeOffheapKind576 Kind { get; }
    internal nuint[] Data { get; }

    internal PreparedExternalIdentifier(ProcessRuntime runtime, Term source)
    {
        if (!NativeWordLayout.Supported)
            throw new PlatformNotSupportedException(ExternalIdentifierLayout.Platform);
        LocalNodeSnapshot snapshot;
        switch (source)
        {
            case Pid pid:
                Kind = NativeOffheapKind576.ExternalPid;
                snapshot = pid.WireIdentity;
                Data = NativeWordLayout.Encode64(pid.Id);
                break;
            case PortTerm port:
                Kind = NativeOffheapKind576.ExternalPort;
                snapshot = port.WireIdentity;
                Data = NativeWordLayout.Encode64(port.Id);
                break;
            case ReferenceTerm reference:
                Kind = NativeOffheapKind576.ExternalReference;
                snapshot = reference.WireIdentity;
                uint[] numbers = reference.Words.ToArray();
                Data = NativeWordLayout.PackUints(new uint[] { (uint)numbers.Length }.Concat(numbers).ToArray());
                break;
            default: throw new InvalidOperationException(ExternalIdentifierLayout.Invalid);
        }
        Node = runtime.KnownNodes.AcquirePhysicalNode(snapshot);
    }

    public void Dispose() => Node.Dispose();
}
