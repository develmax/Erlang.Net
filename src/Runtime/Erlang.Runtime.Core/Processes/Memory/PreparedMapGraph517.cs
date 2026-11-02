using System.Runtime.ExceptionServices;

namespace Erlang;

internal sealed partial class PreparedMapGraph517 : IDisposable
{
    private readonly Dictionary<Term, PreparedExternalIdentifier> externalIdentifiers = new(ReferenceEqualityComparer.Instance);

    internal PreparedExternalIdentifier FindExternalIdentifier(Term source) => externalIdentifiers[source];

    private readonly Dictionary<BinaryBacking, PreparedOrdinaryBinary> ordinaryBinaries = new(ReferenceEqualityComparer.Instance);

    internal PreparedOrdinaryBinary FindOrdinaryBinary(BitString source) => ordinaryBinaries[source.ResourceBacking577];

    private readonly Dictionary<MapTerm, PreparedOwnedMap517> plans = new(ReferenceEqualityComparer.Instance);
    private readonly List<MapRoutingFallback517> fallbacks = new();
    private readonly Dictionary<Term, PreparedOwnedMapNode526> nodes526 = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<PreparedOwnedMap517> extraNodeOwners526 = new(ReferenceEqualityComparer.Instance);

    internal PreparedOwnedMap517? Find(MapTerm map) => plans.GetValueOrDefault(map);

    internal IReadOnlyList<MapRoutingFallback517> Fallbacks => fallbacks;

    internal PreparedMapGraphUse517 AdmitBuilds517() => PreparedMapGraphUse517.Capture(plans.Values.Concat(extraNodeOwners526), bitmaps785.Values);

    private readonly Dictionary<HamtBitmapTerm429, PreparedHamtBitmap785> bitmaps785 = new(ReferenceEqualityComparer.Instance);

    internal PreparedHamtBitmap785? FindBitmap785(Term value) => value is HamtBitmapTerm429 bitmap
        ? bitmaps785.GetValueOrDefault(bitmap) : null;

    internal PreparedOwnedMapNode526? FindNode526(Term value) => nodes526.GetValueOrDefault(value);

    internal void AttachNodes526(PreparedOwnedMap517 plan, bool externallyOwned)
    {
        for (int index = 1; index < plan.Nodes.Length; index++)
        {
            var node = new PreparedOwnedMapNode526(plan, index);
            if (nodes526.TryGetValue(node.Source, out var previous))
            {
                if (previous.Kind != node.Kind || previous.PairCount != node.PairCount
                    || previous.Shape.Bitmap != node.Shape.Bitmap)
                    throw new NotSupportedException(MapRouting517Names.SharedNodeConflict526);
            }
            else
                nodes526.Add(node.Source, node);
        }
        if (externallyOwned)
            extraNodeOwners526.Add(plan);
    }

    internal static PreparedMapGraph517 Capture(
        ProcessTermHeap506 heap,
        ProcessRuntime runtime,
        Term source,
        bool preserveLocal
    )
    {
        if (Monitor.IsEntered(heap.Gate))
            throw new InvalidOperationException(OwnedMapPayloadNames517.PreparationRequired);
        var graph = new PreparedMapGraph517();
        try
        {
            var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
            var bitmapCandidates = new List<HamtBitmapTerm429>();
            var pending = new Stack<Term>();
            pending.Push(source);
            while (pending.TryPop(out Term? value))
            {
                if (!seen.Add(value))
                    continue;
                // Only adoption returns this exact live local node unchanged.
                // False/default imports must retain every actual copying plan.
                if (preserveLocal && ProcessTermHeap506.Storage(value) is { } storage
                    && ReferenceEquals(storage.Owner, heap))
                {
                    lock (heap.Gate)
                        if (heap.IsOwnedLocal(value)) continue;
                }
                if (value is HamtBitmapTerm429 bitmap) bitmapCandidates.Add(bitmap);
                graph.CaptureCanonicalFunction(runtime, value);
                graph.CaptureRecord(runtime, value, pending);
                if (heap.CanOwnExternalIdentifier(value))
                {
                    var plan = new PreparedExternalIdentifier(runtime, value);
                    try
                    {
                        graph.externalIdentifiers.Add(value, plan);
                    }
                    catch
                    {
                        plan.Dispose();
                        throw;
                    }
                }
                if (value is BitString binary && heap.CanOwnOrdinaryBinary(binary)
                    && !ContainsOrdinaryBinary(graph, binary))
                {
                    var plan = new PreparedOrdinaryBinary(runtime, binary, heap);
                    try
                    {
                        graph.ordinaryBinaries.Add(binary.ResourceBacking577, plan);
                    }
                    catch
                    {
                        plan.Dispose();
                        throw;
                    }
                }
                if (value is MapTerm map)
                {
                    if (preserveLocal && heap.IsLocalMapForPreparation517(map))
                        continue;
                    if (map.Entries.Count == 0 && heap.LiteralDomain800 is null)
                    {
                        graph.fallbacks.Add(new(map, MapRouting517Names.EmptyMap, null));
                        continue;
                    }
                    if (map.Entries.Count <= HamtViewLayout.ReleaseFlatMapLimit && map.FlatKeys426 is null)
                    {
                        graph.fallbacks.Add(new(map, MapRouting517Names.MissingFlatKeys, null));
                        continue;
                    }
                    try
                    {
                        var plan = heap.LiteralDomain800 is { IsSealed800: false }
                            ? PreparedOwnedMap517.CaptureModuleLiteral800(heap, runtime, map)
                            : PreparedOwnedMap517.Capture(runtime, map);
                        try
                        {
                            graph.plans.Add(map, plan);
                        }
                        catch (Exception original)
                        {
                            try
                            {
                                plan.Dispose();
                            }
                            catch (Exception cleanup)
                            {
                                throw new AggregateException(original, cleanup);
                            }
                            throw;
                        }
                    }
                    catch (NotSupportedException error)
                    {
                        // Creating/holding the legacy map did not require a new
                        // index. Preserve exactly that path, with the complete
                        // preparation evidence, rather than reject a supported
                        // old producer or normalize hidden/projection rows.
                        graph.fallbacks.Add(new(map, error.Message, error.ToString()));
                        continue;
                    }
                }
                foreach (Term child in NativeMapOwnedRoots510.Children(value))
                    pending.Push(child);
            }

            foreach (var plan in graph.plans.Values)
                graph.AttachNodes526(plan, false);

            foreach (HamtBitmapTerm429 bitmap in bitmapCandidates)
            {
                if (graph.nodes526.ContainsKey(bitmap)) continue;
                var preparedBitmap = new PreparedHamtBitmap785(runtime, bitmap);
                try
                {
                    graph.bitmaps785.Add(bitmap, preparedBitmap);
                }
                catch (Exception primary)
                {
                    try
                    {
                        preparedBitmap.Dispose();
                    }
                    catch (Exception cleanup)
                    {
                        throw new AggregateException(primary, cleanup);
                    }
                    throw;
                }
            }

            return graph;
        }
        catch (Exception original)
        {
            try
            {
                graph.Dispose();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
    }

    private static bool ContainsOrdinaryBinary(PreparedMapGraph517 graph, BitString source)
        => graph.ordinaryBinaries.ContainsKey(source.ResourceBacking577);

    public void Dispose()
    {
        List<Exception>? errors = null;
        foreach (var plan in plans.Values)
            try
            {
                plan.Dispose();
            }
            catch (Exception error)
            {
                (errors ??= new()).Add(error);
            }
        foreach (var binary in ordinaryBinaries.Values)
            try
            {
                binary.Dispose();
            }
            catch (Exception error)
            {
                (errors ??= new()).Add(error);
            }
        foreach (var external in externalIdentifiers.Values)
            try
            {
                external.Dispose();
            }
            catch (Exception error)
            {
                (errors ??= new()).Add(error);
            }
        foreach (var bitmap in bitmaps785.Values)
            try
            {
                bitmap.Dispose();
            }
            catch (Exception error)
            {
                (errors ??= new()).Add(error);
            }
        bitmaps785.Clear();
        records.Clear();
        externalIdentifiers.Clear();
        ordinaryBinaries.Clear();
        plans.Clear();
        fallbacks.Clear();
        nodes526.Clear();
        extraNodeOwners526.Clear();
        if (errors is { Count: 1 })
            ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors is { Count: > 1 })
            throw new AggregateException(errors);
    }
}
