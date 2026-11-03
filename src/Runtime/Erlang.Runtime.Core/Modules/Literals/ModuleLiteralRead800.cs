namespace Erlang;

internal sealed class ModuleLiteralRead800(ModuleLiteralDomain800 domain) : IDisposable
{
    private ModuleLiteralDomain800? owner = domain;

    public void Dispose() => Interlocked.Exchange(ref owner, null)?.ReleaseReader800();
}
