namespace Erlang;

// Capture is deliberately outside the target heap gate. Hash-index admission
// can acquire runtime, KnownNodes and index-owner locks. The committed result
// retains none of this plan's original Term rows or source HamtViewNode objects.
internal sealed class PreparedOwnedMap517 : IDisposable
{
    private HamtIndexRead429? admission;
    private readonly object useGate = new();
    private bool disposed;
    private int users;
    internal ProcessRuntime Runtime { get; }
    internal object Domain { get; }
    internal NifNativeMapHashRetention504? HashRetention { get; }
    internal OwnedMapNodePlan517[] Nodes { get; }
    internal int[] Physical { get; }
    internal int[] Canonical { get; }
    internal int[] Arithmetic { get; }
    internal int[]? FlatKeys { get; }
    internal IReadOnlyList<Term> Children { get; }
    internal int Count => Physical.Length;
    internal bool LogicalOrigin517 { get; }
    internal IReadOnlyList<Term> SourceStoredNodes517 { get; }
    internal IReadOnlyList<Term?> SourceNodeTerms526 { get; }
    internal IReadOnlyList<int> NodePairCounts526 { get; }

    internal static PreparedOwnedMap517 Capture(ProcessRuntime runtime, MapTerm map)
        => CaptureCore800(runtime, map, false);

    internal static PreparedOwnedMap517 CaptureModuleLiteral800(ProcessTermHeap506 heap, ProcessRuntime runtime, MapTerm map)
    {
        if (heap.LiteralDomain800 is not { IsSealed800: false } domain || !ReferenceEquals(domain.Runtime800, runtime))
            throw new InvalidOperationException(ModuleLiteral800Names.Invalid);

        return CaptureCore800(runtime, map, true);
    }

    private static PreparedOwnedMap517 CaptureCore800(ProcessRuntime runtime, MapTerm map, bool moduleLiteral800)
    {
        if (map.Entries.Count == 0 && !moduleLiteral800)
            throw new NotSupportedException(OwnedMapPayloadNames517.EmptyGlobal);
        runtime.RequireMapHashOpen();
        object domain = runtime.NativeMapDomain505();
        if (map.Entries.Count <= HamtViewLayout.ReleaseFlatMapLimit)
        {
            if (NativeMapMaterialization505.TryRead(map, runtime, out var small) && small.Root is not null)
                throw new NotSupportedException(OwnedMapPayloadNames517.InvalidShape);

            return new(
                runtime,
                domain,
                map,
                NativeMapFlatEntries505.Read(map),
                null,
                null
            );
        }
        var read = runtime.AcquireHamtIndex429(map);
        try
        {
            bool preserved = NativeMapMaterialization505.TryRead(map, runtime, out var saved);
            var retention = read.Index.RetainedOwnership as NifNativeMapHashRetention504
                ?? new NifNativeMapHashRetention504(read.Index.Ownership);
            var plan = new PreparedOwnedMap517(
                runtime,
                domain,
                map,
                preserved ? saved.PhysicalEntries : map.Entries,
                read.Index.Root,
                retention
            );
            plan.admission = read;

            return plan;
        }
        catch
        {
            read.Dispose();
            throw;
        }
    }

    private PreparedOwnedMap517(
        ProcessRuntime runtime,
        object domain,
        MapTerm map,
        IReadOnlyList<KeyValuePair<Term, Term>> physical,
        HamtViewNode? root,
        NifNativeMapHashRetention504? retention
    )
    {
        LogicalOrigin517 = map.IsOrdinaryLogicalMap517;
        Runtime = runtime;
        Domain = domain;
        HashRetention = retention;
        var rows = new List<KeyValuePair<Term, Term>>();
        var occurrences = new Dictionary<KeyValuePair<Term, Term>, List<int>>(NativeMapRowReferenceComparer510.Instance);

        int Add(KeyValuePair<Term, Term> row)
        {
            int index = rows.Count;
            rows.Add(row);
            if (!occurrences.TryGetValue(row, out var found))
                occurrences.Add(row, found = new());
            found.Add(index);

            return index;
        }
        var nodes = new List<OwnedMapNodePlan517>();
        var storedNodes = new List<Term>();
        var nodeTerms = new List<Term?>();
        if (root is not null)
        {
            if (!root.IsHead || retention is null)
                throw new NotSupportedException(OwnedMapPayloadNames517.InvalidShape);
            var queue = new List<HamtViewNode> { root };
            for (int index = 0; index < queue.Count; index++)
            {
                var node = queue[index];
                nodeTerms.Add(node.StoredTerm);
                if (node.StoredTerm is { } stored)
                    storedNodes.Add(stored);
                bool leaf = node.Leaf is not null;
                if (leaf && (node.Children.Count != 0 || node.Bitmap != 0 || node.IsHead || node.IsCollision)
                    || node.IsHead && index != 0
                    || node.IsCollision && (node.IsHead || node.Bitmap != 0 || node.Children.Count < 2 || node.Children.Any(child => child.Leaf is null))
                    || !leaf && !node.IsCollision && (node.Children.Count == 0 || node.Children.Count != System.Numerics.BitOperations.PopCount((uint)node.Bitmap)))
                    throw new NotSupportedException(OwnedMapPayloadNames517.InvalidShape);
                int[] childIndices = new int[node.Children.Count];
                for (int child = 0; child < childIndices.Length; child++)
                {
                    childIndices[child] = queue.Count;
                    queue.Add(node.Children[child]);
                }
                nodes.Add(new(
                    node.Leaf is { } pair ? Add(pair) : -1,
                    childIndices,
                    node.Bitmap,
                    node.IsCollision,
                    node.IsHead
                ));
            }
        }
        else
            foreach (var row in physical)
                Add(row);

        int[] View(IReadOnlyList<KeyValuePair<Term, Term>> source)
        {
            var next = new Dictionary<KeyValuePair<Term, Term>, int>(NativeMapRowReferenceComparer510.Instance);
            var view = new int[source.Count];
            for (int index = 0; index < view.Length; index++)
            {
                var pair = source[index];
                int offset = next.GetValueOrDefault(pair);
                // An inconsistent host projection is not a physical map node.
                // Reject before allocation instead of dropping it or inventing
                // extra ERTS map leaves to charge to the process.
                if (!occurrences.TryGetValue(pair, out var found) || offset >= found.Count)
                    throw new NotSupportedException(OwnedMapPayloadNames517.InvalidShape);
                view[index] = found[offset];
                next[pair] = offset + 1;
            }

            return view;
        }
        Physical = View(physical);
        Canonical = View(map.Entries);
        Arithmetic = View(map.ArithmeticKeyEntries);
        if (Physical.Length != rows.Count || Canonical.Length != rows.Count || Arithmetic.Length != rows.Count)
            throw new NotSupportedException(OwnedMapPayloadNames517.InvalidShape);
        if (map.FlatKeys426 is { } keys)
        {
            var byKey = new Dictionary<Term, Queue<int>>(ReferenceEqualityComparer.Instance);
            foreach (int row in Physical)
            {
                Term key = rows[row].Key;
                if (!byKey.TryGetValue(key, out var found))
                    byKey.Add(key, found = new());
                found.Enqueue(row);
            }
            FlatKeys = new int[keys.Items.Count];
            for (int index = 0; index < FlatKeys.Length; index++)
            {
                if (!byKey.TryGetValue(keys.Items[index], out var found) || !found.TryDequeue(out int row))
                    throw new NotSupportedException(OwnedMapPayloadNames517.InvalidShape);
                FlatKeys[index] = row;
            }
            if (FlatKeys.Length != rows.Count || nodes.Count != 0)
                throw new NotSupportedException(OwnedMapPayloadNames517.InvalidShape);
            if (!FlatKeys.SequenceEqual(Physical))
                throw new NotSupportedException(OwnedMapPayloadNames517.InvalidShape);
        }
        else if (root is null)
            throw new NotSupportedException(OwnedMapPayloadNames517.InvalidShape);
        Nodes = nodes.ToArray();
        SourceStoredNodes517 = Array.AsReadOnly(storedNodes.ToArray());
        SourceNodeTerms526 = Array.AsReadOnly(nodeTerms.ToArray());
        var pairCounts = new int[Nodes.Length];
        for (int index = Nodes.Length - 1; index >= 0; index--)
            pairCounts[index] = Nodes[index].Row >= 0 ? 1 : Nodes[index].Children.Sum(child => pairCounts[child]);
        NodePairCounts526 = Array.AsReadOnly(pairCounts);
        // The key tuple is a real graph node, not a synthesized permutation.
        // Visiting it normally lets same-process adoption preserve a separately
        // reachable map_to_tuple_keys result in either operand order.
        Children = FlatKeys is not null
            ? Array.AsReadOnly(new Term[] { map.FlatKeys426! }.Concat(Physical.Select(row => rows[row].Value)).ToArray())
            : Array.AsReadOnly(Nodes[0].Children.Select(child => nodeTerms[child]!).ToArray());
    }

    internal PreparedOwnedMapUse517 EnterUse517()
    {
        lock (useGate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            var use = new PreparedOwnedMapUse517(this);
            users = checked(users + 1);

            return use;
        }
    }

    internal void LeaveUse517()
    {
        HamtIndexRead429? release = null;
        lock (useGate)
        {
            users--;
            if (disposed && users == 0)
            {
                release = admission;
                admission = null;
            }
        }
        release?.Dispose();
    }

    public void Dispose()
    {
        HamtIndexRead429? release = null;
        lock (useGate)
        {
            if (disposed)
                return;
            disposed = true;
            if (users == 0)
            {
                release = admission;
                admission = null;
            }
        }
        release?.Dispose();
    }
}
