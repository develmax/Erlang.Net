namespace Erlang;

public sealed partial class ProcessContext
{
    private BeamSourceLocationCache154? beamSourceLocations154;

    internal BeamSourceLocationCache154 AcquireBeamSourceLocations154()
    {
        ProcessTermHeap506 heap = TermHeap506;
        lock (heap.Gate)
        {
            if (beamSourceLocations154 is { } existing)
            {
                existing.AddReference154(this, heap);

                return existing;
            }
            // Constructor allocates all metadata before its final frame Enter.
            var created = new BeamSourceLocationCache154(this, heap);
            beamSourceLocations154 = created;

            return created;
        }
    }

    internal void ReleaseBeamSourceLocations154(BeamSourceLocationCache154 cache)
    {
        ProcessTermHeap506 heap = TermHeap506;
        bool retire;
        lock (heap.Gate)
        {
            if (!ReferenceEquals(beamSourceLocations154, cache))
                throw new InvalidOperationException();
            retire = cache.RemoveReference154(this, heap);
            if (retire) beamSourceLocations154 = null;
        }
        // Detach under H before disposal; no frame retirement under this lock.
        if (retire) cache.RetireFrame154();
    }
}
