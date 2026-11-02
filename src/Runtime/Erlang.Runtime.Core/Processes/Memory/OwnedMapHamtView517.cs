namespace Erlang;

// An index facade, not a second key/value or child-Term payload. The canonical
// pointer cells are the registered sequence read by StoredTerm and Leaf.
internal sealed class OwnedMapHamtView517(
    ProcessTermHeap506 owner,
    ProcessTermSequence506 payload,
    IReadOnlyList<HamtViewNode> children,
    Term? stored
)
{
    internal bool MatchesOwnedPayload302(ProcessTermHeap506 expectedOwner, ProcessTermSequence506 expectedPayload)
        => ReferenceEquals(owner, expectedOwner) && ReferenceEquals(payload, expectedPayload);

    internal OwnedMapHeader517 Header => owner.ReadMapHeader517(payload);
    internal KeyValuePair<Term, Term>? Leaf => Header.Kind == OwnedMapHeaderKind517.HamtLeafCons
        ? new(payload[0], payload[1]) : null;
    internal IReadOnlyList<HamtViewNode> Children
    {
        get
        {
            _ = Header;

            return children;
        }
    }
    internal Term? StoredTerm
    {
        get
        {
            _ = Header;

            return stored;
        }
    }
}
