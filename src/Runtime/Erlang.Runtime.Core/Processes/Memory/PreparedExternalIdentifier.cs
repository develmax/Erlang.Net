namespace Erlang;

internal sealed class PreparedExternalIdentifier : IDisposable
{
    internal PhysicalNodeLease Node { get; }
    internal NativeOffheapKind576 Kind { get; }
    internal nuint[] Data { get; }

    internal PreparedExternalIdentifier(ProcessRuntime runtime, Term source)
    {
        if (IntPtr.Size != sizeof(ulong) || !BitConverter.IsLittleEndian)
            throw new PlatformNotSupportedException(ExternalIdentifierLayout.Platform);
        LocalNodeSnapshot snapshot;
        switch (source)
        {
            case Pid pid:
                Kind = NativeOffheapKind576.ExternalPid;
                snapshot = pid.WireIdentity;
                Data = [(nuint)pid.Id];
                break;
            case PortTerm port:
                Kind = NativeOffheapKind576.ExternalPort;
                snapshot = port.WireIdentity;
                Data = [(nuint)port.Id];
                break;
            case ReferenceTerm reference:
                Kind = NativeOffheapKind576.ExternalReference;
                snapshot = reference.WireIdentity;
                uint[] numbers = reference.Words.ToArray();
                Data = new nuint[numbers.Length / 2 + 1];
                Data[0] = (nuint)numbers.Length;
                for (int index = 0; index < numbers.Length; index++)
                    Data[(index + 1) / 2] |= (nuint)numbers[index] << ((index + 1) % 2 * ExternalIdentifierLayout.UintBits);
                break;
            default: throw new InvalidOperationException(ExternalIdentifierLayout.Invalid);
        }
        Node = runtime.KnownNodes.AcquirePhysicalNode(snapshot);
    }

    public void Dispose() => Node.Dispose();
}
