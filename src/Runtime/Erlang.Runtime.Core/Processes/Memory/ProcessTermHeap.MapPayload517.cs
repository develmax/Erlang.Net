namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    // Structural headers are genuine stored state, not a calculated footprint.
    // They retire with the corresponding registered payload block. They are
    // deliberately excluded from the old private payload-word snapshot.
    private readonly Dictionary<long, OwnedMapHeader517> mapHeaders517 = new();

    internal PreparedOwnedMap517 PrepareMap517(MapTerm source)
    {
        if (Monitor.IsEntered(Gate))
            throw new InvalidOperationException(OwnedMapPayloadNames517.PreparationRequired);

        return PreparedOwnedMap517.Capture(runtime, source);
    }

    internal OwnedMapHeader517 ReadMapHeader517(ProcessTermSequence506 payload)
    {
        lock (Gate)
        {
            if (!ReferenceEquals(payload.Owner, this)
                || !blocks.TryGetValue(payload.Identity, out var active) || !ReferenceEquals(active, payload)
                || !mapHeaders517.TryGetValue(payload.Identity, out var header))
                throw new ObjectDisposedException(OwnedMapPayloadNames517.HeaderMissing);

            int encodedLength = EncodedMapHeader526.WordCount(header);
            if (encodedLength != payload.HeaderWords526
                || encodedLength > 0 && payload.Slab.Cells.ReadHeader526(payload.AllocationOffset526) != EncodedMapHeader526.First(header)
                || encodedLength == 2 && payload.Slab.Cells.ReadHeader526(payload.AllocationOffset526 + 1) != checked((nuint)header.PairCount))
                throw new InvalidOperationException(CellStorage526Names.InvalidHeader);

            return header;
        }
    }

    private ProcessTermSequence506 AllocateMap517(
        Term[] cells,
        OwnedMapHeader517 header,
        List<ProcessTermSequence506> transaction
    )
    {
        if (cells.Length == 0 || header.PayloadArity != cells.Length)
            throw new InvalidOperationException(OwnedMapPayloadNames517.InvalidShape);
        transaction.EnsureCapacity(checked(transaction.Count + 1));
        mapHeaders517.EnsureCapacity(checked(mapHeaders517.Count + 1));
        var payload = AllocateEncoded526(cells, EncodedMapHeader526.Words(header));
        transaction.Add(payload);
        mapHeaders517.Add(payload.Identity, header);

        return payload;
    }

    internal MapTerm CreateMap517(PreparedOwnedMap517 prepared, ProcessTermFrame506 construction, Term[] copiedChildren)
    {
        if (Monitor.IsEntered(Gate))
            throw new InvalidOperationException(OwnedMapPayloadNames517.PreparationRequired);
        using var admission = prepared.EnterUse517();
        var inputs = (Term[])copiedChildren.Clone();
        if (inputs.Length != prepared.Children.Count)
            throw new InvalidOperationException(OwnedMapPayloadNames517.InvalidShape);
        var input = new TupleTerm(inputs);
        ProcessTermConstruction514 custody = construction.BeginConstruction514(input);
        using PreparedMapGraph517 maps = PrepareMapGraph517(input, true);
        maps.AttachNodes526(prepared, true);
        using PreparedMapGraphUse517 admitted = maps.AdmitBuilds517();
        lock (Gate)
        {
            var copied = (TupleTerm)AdoptGuest507(
                input,
                custody,
                maps,
                admitted
            );

            return CreateMapWithCustody517(
                prepared,
                custody,
                copied.Items.ToArray(),
                admission
            );
        }
    }

    internal MapTerm CreateMapWithCustody517(
        PreparedOwnedMap517 prepared,
        ProcessTermConstruction514 construction,
        Term[] copiedChildren,
        PreparedOwnedMapUse517 admission
    )
    {
        // Caller acquired this before entering every enclosing heap gate.
        // It is released only after the final slot has been published.
        if (!admission.IsFor517(prepared))
            throw new ObjectDisposedException(nameof(PreparedOwnedMapUse517));
        if (!ReferenceEquals(prepared.Runtime, runtime))
            throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
        var copied = (Term[])copiedChildren.Clone();
        if (copied.Length != prepared.Children.Count
            || copied.Length != (prepared.FlatKeys is null ? prepared.Nodes[0].Children.Length : checked(prepared.Count + 1)))
            throw new InvalidOperationException(OwnedMapPayloadNames517.InvalidShape);
        lock (Gate)
        {
            if (closing || !frames.Contains(construction.Frame517) || construction.Frame517.IsDisposed)
                throw new ObjectDisposedException(OwnedMapPayloadNames517.ConstructionRequired);
            using var provisional = Enter();
            ProcessTermConstruction514 fork = provisional.BeginMapFork517(construction, copied);
            var allocated = new List<ProcessTermSequence506>();
            try
            {
                var rows = new OwnedMapRow517[prepared.Count];
                ProcessTermSequence506 rootPayload;
                TupleTerm? flatKeys = null;
                HamtViewNode? hamtRoot = null;
                if (prepared.FlatKeys is { } keyOrder)
                {
                    if (!keyOrder.SequenceEqual(prepared.Physical))
                        throw new NotSupportedException(OwnedMapPayloadNames517.InvalidShape);
                    flatKeys = copied[0] as TupleTerm
                        ?? throw new InvalidOperationException(OwnedMapPayloadNames517.InvalidShape);
                    if (!IsOwnedLocal(flatKeys) || flatKeys.OwnedStorage506 is not ProcessTermSequence506 keysPayload
                        || keysPayload.Count != prepared.Count)
                        throw new InvalidOperationException(OwnedMapPayloadNames517.InvalidShape);
                    var body = new Term[checked(prepared.Count + 1)];
                    body[0] = flatKeys;
                    for (int index = 0; index < prepared.Physical.Length; index++)
                        body[index + 1] = copied[index + 1];
                    rootPayload = AllocateMap517(body,
                        new(
                            OwnedMapHeaderKind517.FlatMap,
                            body.Length,
                            prepared.Count,
                            0
                        ), allocated);
                    for (int index = 0; index < prepared.Physical.Length; index++)
                        rows[prepared.Physical[index]] = new(
                            keysPayload,
                            index,
                            rootPayload,
                            index + 1
                        );
                }
                else
                {
                    (rootPayload, hamtRoot) = BuildSharedHamt526(
                        prepared,
                        copied,
                        allocated,
                        rows
                    );
                }
                var payloadView = new OwnedMapPayload517(
                    rootPayload,
                    rows,
                    (int[])prepared.Physical.Clone(),
                    (int[])prepared.Canonical.Clone(),
                    (int[])prepared.Arithmetic.Clone()
                );
                MapTerm map = NativeMapMaterialization505.PublishOwned517(
                    runtime,
                    prepared.Domain,
                    payloadView,
                    flatKeys,
                    hamtRoot,
                    prepared.HashRetention,
                    prepared.LogicalOrigin517
                );
                construction.Publish(map);
                fork.CommitFork517();

                return map;
            }
            catch
            {
                // No partially constructed map is exposed; retire provisional
                // roots before rolling back their payload/header allocations.
                fork.RollbackFork517();
                provisional.Dispose();
                foreach (var payload in allocated)
                {
                    // Reset every per-length slab hint when rollback opens an
                    // earlier hole, just like collection and base allocation.
                    ReleaseAllocation519(payload.Slab, payload.AllocationOffset526, payload.AllocationWords526);
                    blocks.Remove(payload.Identity);
                    oldBlocks508.Remove(payload.Identity);
                    mapHeaders517.Remove(payload.Identity);
                }
                throw;
            }
        }
    }
}
