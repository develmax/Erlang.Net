namespace Erlang;

internal sealed class OwnedMapPayload517 : IOwnedMapPayload517
{
    public IOwnedTermSequence506 RootPayload { get; }
    public IReadOnlyList<KeyValuePair<Term, Term>> Physical { get; }
    public IReadOnlyList<KeyValuePair<Term, Term>> Canonical { get; }
    public IReadOnlyList<KeyValuePair<Term, Term>> Arithmetic { get; }

    internal OwnedMapPayload517(
        ProcessTermSequence506 root,
        OwnedMapRow517[] rows,
        int[] physical,
        int[] canonical,
        int[] arithmetic
    )
    {
        RootPayload = root;
        var owner = (ProcessTermHeap506)root.Owner;
        Physical = new OwnedMapEntries517(
            owner,
            root,
            rows,
            physical
        );
        Canonical = new OwnedMapEntries517(
            owner,
            root,
            rows,
            canonical
        );
        Arithmetic = new OwnedMapEntries517(
            owner,
            root,
            rows,
            arithmetic
        );
    }
}
