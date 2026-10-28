using System.Collections;

namespace Erlang;

internal sealed class ProcessTermSequence506(
    ProcessTermHeap506 owner,
    long identity,
    ProcessTermSlab506 slab,
    int offset,
    int length,
    int headerWords526 = 0,
    int rawPayloadWords556 = 0
) : IOwnedFloatStorage556, IOwnedIntegerStorage559, IOwnedReferenceStorage562
{
    public object Owner => owner;
    public long Identity { get; } = identity;
    public int Count { get; } = length;
    internal ProcessTermSlab506 Slab { get; } = slab;
    internal int Offset { get; } = offset;
    internal int HeaderWords526 { get; } = headerWords526;
    internal int RawPayloadWords556 { get; } = rawPayloadWords556;
    internal int AllocationOffset526 => Offset - HeaderWords526 - RawPayloadWords556;
    internal int AllocationWords526 => checked(Count + HeaderWords526 + RawPayloadWords556);
    public Term this[int index] => owner.Read(this, index);

    public double ReadFloat556() => owner.ReadFloat556(this);

    public System.Numerics.BigInteger ReadInteger559() => owner.ReadInteger559(this);

    public IReadOnlyList<uint> ReadReferenceWords562() => owner.ReadReferenceWords562(this);

    public IEnumerator<Term> GetEnumerator()
    {
        for (int index = 0; index < Count; index++)
            yield return this[index];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
