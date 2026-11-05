namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal Action<RuntimeMapSourceSortEvent>? MapSourceSortObserver { get; set; }

    internal MapTerm BuildSourceBulkMap(IReadOnlyList<KeyValuePair<Term, Term>> pairs)
    {
        HamtIndexOwner429 owner;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(hamtAdmissionClosed429 || disposed || IsHaltRequested, this);
            owner = hamtIndex429 ??= new(this);
        }

        return owner.BuildSourceBulk(pairs, MapSourceSortObserver);
    }
}
