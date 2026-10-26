using System.Collections.Concurrent;

namespace Erlang;

internal sealed unsafe partial class NifNativeCallEnvironment
{
    private readonly ConcurrentDictionary<nuint, NifNativeMapIterator504State> mapIterators504 = new();
    private readonly ConcurrentDictionary<object, IReadOnlyList<KeyValuePair<NifNativeTermValue, NifNativeTermValue>>> mapIteratorEntries504
        = new(ReferenceEqualityComparer.Instance);
    private long nextMapIterator504;

    internal int MapIteratorCount504 => mapIterators504.Count;

    internal bool TryMapIteratorEntries504(
        NifNativeTermValue map,
        out IReadOnlyList<KeyValuePair<NifNativeTermValue, NifNativeTermValue>> entries
    )
    {
        _ = Pointer;
        object? identity = (object?)map.Pending ?? map.StaticTerm;
        if (identity is not null && mapIteratorEntries504.TryGetValue(identity, out entries!))
            return true;
        if (!NifNativeMapGraph504.TryEntries(map, Runtime, out entries!))
            return false;
        entries = mapIteratorEntries504.GetOrAdd(identity!, Array.AsReadOnly(entries.ToArray()));

        return true;
    }

    internal nuint AddMapIterator504(NifNativeMapIterator504State state)
    {
        _ = Pointer;
        long identity = Interlocked.Increment(ref nextMapIterator504);
        if (identity <= 0 || !mapIterators504.TryAdd((nuint)identity, state))
            throw new InvalidOperationException(NifNativeMapIterator504Names.StateExhausted);

        return (nuint)identity;
    }

    internal NifNativeMapIterator504State GetMapIterator504(nuint identity, nuint map)
    {
        _ = Pointer;
        if (!mapIterators504.TryGetValue(identity, out var state) || state.Map != map)
            throw new InvalidOperationException(NifNativeMapIterator504Names.InvalidState);
        _ = GetValue(map);

        return state;
    }

    internal void RemoveMapIterator504(nuint identity, nuint map)
    {
        _ = GetMapIterator504(identity, map);
        if (!mapIterators504.TryRemove(identity, out _))
            throw new InvalidOperationException(NifNativeMapIterator504Names.InvalidState);
    }

    private void ClearMapIterators504()
    {
        mapIterators504.Clear();
        mapIteratorEntries504.Clear();
    }
}
