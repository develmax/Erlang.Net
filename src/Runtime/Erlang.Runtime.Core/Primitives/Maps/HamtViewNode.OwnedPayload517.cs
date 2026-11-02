namespace Erlang;

internal sealed partial class HamtViewNode
{
    private readonly OwnedMapHamtView517? owned517;

    internal bool MatchesOwnedPayload302(ProcessTermHeap506 owner, ProcessTermSequence506 payload)
        => owned517?.MatchesOwnedPayload302(owner, payload) == true;

    internal HamtViewNode(OwnedMapHamtView517 owned)
    {
        owned517 = owned;
        children = Array.Empty<HamtViewNode>();
    }
}
