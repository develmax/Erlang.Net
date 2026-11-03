namespace Erlang.Tests;

internal static class OriginalCounters610Compare
{
    internal static OriginalCounters610Result Compare(byte[] original, byte[] managed)
    {
        OriginalCounters610Observation? source = null, target = null;
        try
        {
            source = OriginalCounters610Protocol.Read(ExternalTermFormat.Decode(original));
            target = OriginalCounters610Protocol.Read(ExternalTermFormat.Decode(managed));
            OriginalCounters610Protocol.Require(source.Profile == target.Profile);
            var a = new OriginalCounters610Wire(original, source.Roles).Read();
            var b = new OriginalCounters610Wire(managed, target.Roles).Read();
            OriginalCounters610Protocol.Require(a.Count == b.Count && a.Count >= 6);
            OriginalCounters610Protocol.Require(a.Select(x => x.Role).SequenceEqual(b.Select(x => x.Role)));
            OriginalCounters610Protocol.Require(a.Select(x => x.Role).ToHashSet(StringComparer.Ordinal)
                .SetEquals(OriginalCounters610Names.RoleNames));
            int ax = 0, bx = 0;
            for (int index = 0; index < a.Count; index++)
            {
                OriginalCounters610Protocol.Require(original.AsSpan(ax, a[index].Offset - ax)
                    .SequenceEqual(managed.AsSpan(bx, b[index].Offset - bx)));
                ax = a[index].Offset + OriginalCounters610Names.OpaqueReferenceBytes;
                bx = b[index].Offset + OriginalCounters610Names.OpaqueReferenceBytes;
            }
            OriginalCounters610Protocol.Require(original.AsSpan(ax).SequenceEqual(managed.AsSpan(bx)));

            return new(
                true,
                source.Profile,
                target.Profile,
                Describe(source.Roles),
                Describe(target.Roles),
                a.Select(x => x.Role).ToArray(),
                null
            );
        }
        catch (Exception error)
        {
            return new(
                false,
                source?.Profile,
                target?.Profile,
                source is null ? null : Describe(source.Roles),
                target is null ? null : Describe(target.Roles),
                null,
                error.ToString()
            );
        }
    }

    internal static bool SameBinding(OriginalCounters610Result a, OriginalCounters610Result b)
        => a.Passed && b.Passed && a.OriginalProfile == b.OriginalProfile && a.ManagedProfile == b.ManagedProfile
            && Same(a.OriginalRoles!, b.OriginalRoles!) && Same(a.ManagedRoles!, b.ManagedRoles!)
            && a.OccurrenceRoles!.SequenceEqual(b.OccurrenceRoles!);

    private static bool Same(IReadOnlyDictionary<string, OriginalAtomics590Role> a, IReadOnlyDictionary<string, OriginalAtomics590Role> b)
        => a.Count == b.Count && a.All(pair => b.TryGetValue(pair.Key, out var other)
            && pair.Value.Node == other.Node && pair.Value.Creation == other.Creation
            && pair.Value.Words.SequenceEqual(other.Words));

    private static IReadOnlyDictionary<string, OriginalAtomics590Role> Describe(IReadOnlyDictionary<string, ReferenceTerm> roles)
        => roles.ToDictionary(
            pair => pair.Key,
            pair => new OriginalAtomics590Role(pair.Value.Node, pair.Value.Creation, pair.Value.Words.ToArray()),
            StringComparer.Ordinal
        );
}
