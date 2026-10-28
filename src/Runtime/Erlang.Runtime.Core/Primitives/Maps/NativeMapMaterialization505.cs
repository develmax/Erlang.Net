using System.Runtime.CompilerServices;

namespace Erlang;

internal static class NativeMapMaterialization505
{
    private static readonly ConditionalWeakTable<MapTerm, NativeMapMaterializedLayout505> layouts = new();

    internal static MapTerm Build(
        ProcessRuntime runtime,
        IReadOnlyList<KeyValuePair<Term, Term>> physicalEntries,
        NifNativeMapBranch504? pendingShape,
        NifNativeMapHashRetention504? retention,
        IReadOnlyDictionary<NifNativeTermNode, Term> resolved
    )
    {
        object domain = runtime.NativeMapDomain505();
        HamtViewNode? root = pendingShape is null ? null
            : Materialize(
                pendingShape,
                resolved,
                domain,
                retention!,
                true
            );
        var layout = new NativeMapMaterializedLayout505(
            runtime,
            domain,
            physicalEntries,
            root,
            retention
        );
        // A currently valid HAMT can acquire stale paths after an identity epoch
        // changes. All native HAMTs therefore keep their weak-owner raw lookup;
        // ordinary MapTerm constructors and all flat lookup remain unchanged.
        Func<Term, (bool Found, Term? Value)>? lookup = root is null ? null : layout.Lookup;
        var map = MapTerm.FromNativeLayout504(physicalEntries, lookup, layout.EqualityPlan);
        layouts.Add(map, layout);

        return map;
    }

    internal static bool TryRead(MapTerm map, ProcessRuntime? runtime, out NativeMapMaterializedLayout505 layout)
    {
        if (layouts.TryGetValue(map, out layout!) && (runtime is null || layout.Matches(runtime)))
            return true;
        layout = null!;

        return false;
    }

    internal static bool TryGet(
        MapTerm map,
        Term key,
        ProcessRuntime runtime,
        out Term? value
    )
    {
        if (!layouts.TryGetValue(map, out var layout) || layout.Root is null || layout.Matches(runtime))
            return map.TryGet(key, out value);

        // Host transfer into another VM uses that VM's cached index and hash
        // domain. The original owner may already have completed shutdown.
        using var index = runtime.AcquireHamtIndex429(map);
        using var hashes = runtime.MapHashIdentityProvider(OtpMapHashLayoutProtocol.Append);

        return HamtIndexLookup429.TryGet(
            index.Index,
            key,
            hashes.Hash(key),
            out value
        );
    }

    // Called while the index owner's admission gate is held. Do not acquire the
    // runtime gate here: shutdown takes runtime gate before index-owner gate.
    internal static bool TryIndex(
        MapTerm map,
        ProcessRuntime runtime,
        object domain,
        out HamtIndex429 index
    )
    {
        if (layouts.TryGetValue(map, out var layout) && layout.Root is not null && layout.Matches(runtime, domain))
        {
            index = layout.Index();

            return true;
        }
        index = null!;

        return false;
    }

    private static HamtViewNode Materialize(
        NifNativeMapBranch504 node,
        IReadOnlyDictionary<NifNativeTermNode, Term> resolved,
        object domain,
        NifNativeMapHashRetention504 ownership,
        bool head = false
    ) => node.Entry is { } entry
        ? new(
            new(entry.Key.FromCache(resolved), entry.Value.FromCache(resolved)),
            [],
            domain: domain,
            ownership: ownership
        )
        : new(
            null,
            node.Children.Select(child => Materialize(
                child,
                resolved,
                domain,
                ownership
            )).ToArray(),
            node.Bitmap,
            node.Collision,
            domain,
            ownership,
            head
        );
}
