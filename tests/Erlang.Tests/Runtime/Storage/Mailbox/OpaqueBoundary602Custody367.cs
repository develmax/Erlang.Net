using System.Collections;
using System.Runtime.CompilerServices;
using System.Text.Json;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.OpaqueBoundary602Names;
using static Erlang.Tests.OpaqueBoundary602Custody367Names;

namespace Erlang.Tests;

// Exact real interests, not a public process-memory metric: the snapshot
// graph owns a typed lease AND its recipient fragment owns a physical header.
// Reads inspect registered CLR custody only, granting no native access authority.
internal static class OpaqueBoundary602Custody367
{
    internal static void Check(
        OpaqueBoundary602Atomics atomics,
        object sourceHeap,
        object[] works,
        int messages
    )
    {
        var sourceLeases = HeaderLeases(sourceHeap, atomics);
        var allFragmentLeases = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var allGraphLeases = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var heaps = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var rows = new List<object>();
        bool completeRoute = true;
        foreach (object work in works)
        {
            object? graph = Property(work, Graph);
            object? root = graph is null ? null : Property(graph, RootWork);
            var graphLeases = new HashSet<object>(ReferenceEqualityComparer.Instance);
            if (root is not null) Leaves(OpaqueBoundary602Atomics.Field(root, Backing), atomics, graphLeases);
            allGraphLeases.UnionWith(graphLeases);
            object? fragment = Property(work, RecipientFragment);
            object? heap = fragment is null ? null : Property(fragment, HeapOwner);
            var headerLeases = heap is null ? [] : HeaderLeases(heap, atomics);
            completeRoute &= graphLeases.Count == 1 && fragment is not null && headerLeases.Count != 0;
            allFragmentLeases.UnionWith(headerLeases);
            if (heap is not null) heaps.Add(heap);
            rows.Add(new
            {
                WorkType = work.GetType().FullName,
                HasGraph = graph is not null,
                GraphGuestLeaseLeaves = graphLeases.Count,
                HasFragment = fragment is not null,
                FragmentHeap = heap is null ? 0 : RuntimeHelpers.GetHashCode(heap),
                FragmentRegisteredGuestHeaderLeases = headerLeases.Count
            });
        }
        long actualGuest = atomics.Count(GuestRoots);
        long actualHost = atomics.Count(HostRoots);
        long actualProducer = atomics.Count(Producers);
        bool valid = completeRoute && sourceLeases.Count == 0 && works.Length == messages
            && allGraphLeases.Count == messages && allFragmentLeases.Count == messages
            && actualGuest == allGraphLeases.Count + allFragmentLeases.Count
            && actualHost == 0 && actualProducer == 0;
        if (!valid) throw new InvalidOperationException(Failure + JsonSerializer.Serialize(new
        {
            Messages = messages,
            ActualGuest = actualGuest,
            ActualHost = actualHost,
            ActualProducer = actualProducer,
            SourceRegisteredGuestHeaderLeases = sourceLeases.Count,
            UniqueGraphGuestLeaseLeaves = allGraphLeases.Count,
            UniqueFragmentGuestHeaderLeases = allFragmentLeases.Count,
            UniqueFragmentHeaps = heaps.Count,
            Works = rows
        }));
    }

    internal static HashSet<object> HeaderLeases(object heap, OpaqueBoundary602Atomics atomics)
    {
        var result = new HashSet<object>(ReferenceEqualityComparer.Instance);
        lock (Property(heap, HeapGate))
        {
            var nodes = (IDictionary)OpaqueBoundary602Atomics.Field(heap, MagicReferences)!;
            foreach (object node in nodes.Values)
                if (ReferenceEquals(Property(node, AtomicsRoot), atomics.Root)
                    && OpaqueBoundary602Atomics.Field(node, Retained) is { } lease && (bool)Call(lease, IsFor, atomics.Array)!)
                    result.Add(lease);
        }

        return result;
    }

    private static void Leaves(object? value, OpaqueBoundary602Atomics atomics, HashSet<object> result)
    {
        if (value is null) return;
        if (value.GetType().FullName == GuestLeaseType)
        {
            if (Property(value, Kind).ToString() == Guest && (bool)Call(value, IsFor, atomics.Array)!) result.Add(value);
        }
        else if (value.GetType().FullName == RootSetType && OpaqueBoundary602Atomics.Field(value, Owned) is IEnumerable leaves)
            foreach (object leaf in leaves) Leaves(leaf, atomics, result);
    }
}
