namespace Erlang;

internal sealed class RuntimeEmptyTupleRead803(RuntimeEmptyTupleDomain803 domain) : IDisposable
{
    private RuntimeEmptyTupleDomain803? owner = domain;

    public void Dispose() => Interlocked.Exchange(ref owner, null)?.ReleaseRead803();
}
