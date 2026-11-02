namespace Erlang;

// A real index-owner reader: logical VM close joins outstanding standalone
// cursor preparation exactly as it joins whole-map preparations.
internal sealed class HamtNodeRead785(HamtIndexOwner429 owner) : IDisposable
{
    private HamtIndexOwner429? current = owner;

    public void Dispose() => Interlocked.Exchange(ref current, null)?.Release();
}
