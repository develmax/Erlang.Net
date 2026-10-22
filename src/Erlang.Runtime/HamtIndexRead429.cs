namespace Erlang;

internal sealed class HamtIndexRead429 : IDisposable
{
    private HamtIndexOwner429? owner;
    internal HamtIndex429 Index { get; }

    internal HamtIndexRead429(HamtIndexOwner429 owner, HamtIndex429 index)
    {
        this.owner = owner;
        Index = index;
    }

    public void Dispose() => Interlocked.Exchange(ref owner, null)?.Release();
}
