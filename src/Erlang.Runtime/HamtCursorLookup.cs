namespace Erlang;

internal sealed class HamtCursorLookup(MapTerm map, OtpMapRuntimeIdentityProvider? scope)
{
    private Dictionary<Term, (ulong Hash, ulong Mask)>? paths;

    internal bool TryGet(Term key, out Term? value)
    {
        if (!map.TryGet(key, out value))
            return false;
        if (scope is null)
            return true;

        // OTP's ordered-key cursor performs a HAMT lookup. Equal external
        // references can retain different stored counts and internal hashes.
        // OTP compares equality as soon as it reaches a leaf: unused hash bits
        // must not reject equal keys that followed the same occupied path.
        paths ??= Plan(map.Entries.Select(entry => new HamtViewEntry(entry, scope.Hash(entry.Key))).ToArray());
        var path = paths[key];

        return (path.Hash & path.Mask) == (scope.Hash(key) & path.Mask);
    }

    private static Dictionary<Term, (ulong Hash, ulong Mask)> Plan(IReadOnlyList<HamtViewEntry> entries)
    {
        var result = new Dictionary<Term, (ulong Hash, ulong Mask)>();
        Append(entries, 0, result);

        return result;
    }

    private static void Append(
        IReadOnlyList<HamtViewEntry> entries,
        int level,
        Dictionary<Term, (ulong Hash, ulong Mask)> result
    )
    {
        if (entries.Count == 1 || level == HamtViewLayout.HashLevels)
        {
            ulong mask = level == HamtViewLayout.HashLevels
                ? ulong.MaxValue : (1UL << (level * HamtViewLayout.NibbleBits)) - 1;
            foreach (var entry in entries)
                result.Add(entry.Pair.Key, (entry.Hash, mask));

            return;
        }
        foreach (var group in entries.GroupBy(entry => (entry.Hash >> (level * HamtViewLayout.NibbleBits)) & HamtViewLayout.NibbleMask))
            Append(group.ToArray(), level + 1, result);
    }
}
