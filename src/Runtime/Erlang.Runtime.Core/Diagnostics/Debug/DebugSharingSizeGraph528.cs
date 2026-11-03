namespace Erlang;

internal sealed class DebugSharingSizeGraph528(ProcessRuntime runtime) : IDisposable
{
    private readonly Dictionary<MapTerm, HamtIndexRead429> maps = new(ReferenceEqualityComparer.Instance);

    private HamtViewNode Root(MapTerm map)
    {
        if (!maps.TryGetValue(map, out var read))
        {
            read = runtime.AcquireHamtIndex429(map);
            try
            {
                maps.Add(map, read);
            }
            catch
            {
                read.Dispose();
                throw;
            }
        }

        return read.Index.Root;
    }

    internal ulong OwnWords(Term value, bool shared)
    {
        ulong own;
        if (value is MapTerm map && map.Entries.Count > HamtViewLayout.ReleaseFlatMapLimit)
            own = checked(DebugSharingSizePolicy528.HamtHeadWords + (ulong)Root(map).Children.Count);
        else
            own = DebugSharingWordLayout425.OwnWords(value);
        bool hamt = value is HamtBitmapTerm429
            || value is MapTerm large && large.Entries.Count > HamtViewLayout.ReleaseFlatMapLimit;

        return shared && hamt ? DebugSharingSizePolicy528.SharedHamtWords(own) : own;
    }

    internal Term[] Children(Term value)
        => value is MapTerm map && map.Entries.Count > HamtViewLayout.ReleaseFlatMapLimit
            ? Root(map).Children.Select(child => child.StoredTerm!).ToArray()
            : DebugSharingGraph425.Children(value);

    public void Dispose()
    {
        foreach (HamtIndexRead429 read in maps.Values)
            read.Dispose();
        maps.Clear();
    }
}
