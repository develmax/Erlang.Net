using System.Collections;

namespace Erlang;

internal sealed class OwnedMapEntries517(
    ProcessTermHeap506 owner,
    ProcessTermSequence506 root,
    OwnedMapRow517[] rows,
    int[] order
) : IReadOnlyList<KeyValuePair<Term, Term>>
{
    public int Count => owner.ReadMapHeader517(root).PairCount;
    public KeyValuePair<Term, Term> this[int index]
    {
        get
        {
            _ = owner.ReadMapHeader517(root);

            return rows[order[index]].Read();
        }
    }

    public IEnumerator<KeyValuePair<Term, Term>> GetEnumerator()
    {
        for (int index = 0; index < Count; index++)
            yield return this[index];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
