namespace Erlang;

internal sealed partial class HamtViewNode
{
    private readonly OwnedMapHamtView517? owned517;

    internal HamtViewNode(OwnedMapHamtView517 owned)
    {
        owned517 = owned;
        children = Array.Empty<HamtViewNode>();
    }
}
