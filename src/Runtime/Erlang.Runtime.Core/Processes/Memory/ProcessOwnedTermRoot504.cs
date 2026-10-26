namespace Erlang;

internal sealed class ProcessOwnedTermRoot504(
    ProcessOwnedTermHeap504 owner,
    long lease,
    ProcessOwnedTermId504 id
) : IDisposable
{
    internal ProcessOwnedTermHeap504 Owner { get; } = owner;
    internal long Lease { get; } = lease;
    internal ProcessOwnedTermId504 Id { get; } = id;

    internal byte[] ReadBinary() => Owner.ReadBinary(this);

    internal ProcessOwnedTermId504[] ReadReferences() => Owner.ReadReferences(this);

    internal void WriteReference(int index, ProcessOwnedTermId504 value) => Owner.WriteReference(this, index, value);

    public void Dispose() => Owner.Release(this);
}
