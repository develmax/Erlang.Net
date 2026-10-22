namespace Erlang;

internal sealed partial class RuntimeEtsStoredObject
{
    internal void AddOwnedCensus420(EtsOwnedCensus420 census)
    {
        lock (gate)
        {
            if (!census.Add(this)) return;
            census.Add(gate);
            if (payload is null) throw new ObjectDisposedException(nameof(RuntimeEtsStoredObject));
            payload.AddOwnedCensus420(census);
        }
    }
}
