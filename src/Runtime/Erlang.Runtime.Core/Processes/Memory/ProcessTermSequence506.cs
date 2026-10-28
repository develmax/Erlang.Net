using System.Collections;

namespace Erlang;

internal sealed class ProcessTermSequence506(
    ProcessTermHeap506 owner,
    long identity,
    ProcessTermSlab506 slab,
    int offset,
    int length
) : IOwnedTermSequence506
{
    public object Owner => owner;
    public long Identity { get; } = identity;
    public int Count { get; } = length;
    internal ProcessTermSlab506 Slab { get; } = slab;
    internal int Offset { get; } = offset;
    public Term this[int index] => owner.Read(this, index);

    public IEnumerator<Term> GetEnumerator()
    {
        for (int index = 0; index < Count; index++)
            yield return this[index];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
