namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    internal Term CreateSharedMapNode526(
        PreparedOwnedMapNode526 prepared,
        ProcessTermConstruction514 construction,
        Term[] copiedChildren,
        PreparedOwnedMapUse517 admission
    )
    {
        if (!admission.IsFor517(prepared.Owner) || !ReferenceEquals(prepared.Owner.Runtime, runtime))
            throw new InvalidOperationException(OwnedMapPayloadNames517.PreparationRequired);
        Term[] cells = (Term[])copiedChildren.Clone();
        int expected = prepared.Shape.Row >= 0 ? 2 : prepared.Shape.Children.Length;
        if (cells.Length != expected)
            throw new InvalidOperationException(OwnedMapPayloadNames517.InvalidShape);
        lock (Gate)
        {
            if (closing || !frames.Contains(construction.Frame517) || construction.Frame517.IsDisposed)
                throw new ObjectDisposedException(OwnedMapPayloadNames517.ConstructionRequired);
            foreach (Term child in cells)
                if (!construction.Contains(child))
                    throw new InvalidOperationException(ProcessTermConstruction514Names.UnadmittedChild);
            var allocated = new List<ProcessTermSequence506>();
            try
            {
                var payload = AllocateMap517(cells,
                    new(
                        prepared.Kind,
                        cells.Length,
                        prepared.PairCount,
                        prepared.Shape.Bitmap
                    ), allocated);
                Term value = prepared.Kind switch
                {
                    OwnedMapHeaderKind517.HamtLeafCons => new Cons(payload),
                    OwnedMapHeaderKind517.HamtCollision => new TupleTerm(payload),
                    OwnedMapHeaderKind517.HamtBitmap when prepared.Source is HamtBitmapTerm429 source =>
                        new HamtBitmapTerm429(
                            source.Domain,
                            source.Ownership,
                            source.Bitmap,
                            payload
                        ),
                    _ => throw new InvalidOperationException(OwnedMapPayloadNames517.InvalidShape)
                };
                construction.Publish(value);

                return value;
            }
            catch
            {
                foreach (ProcessTermSequence506 payload in allocated)
                {
                    ReleaseAllocation519(payload.Slab, payload.AllocationOffset526, payload.AllocationWords526);
                    blocks.Remove(payload.Identity);
                    oldBlocks508.Remove(payload.Identity);
                    mapHeaders517.Remove(payload.Identity);
                }
                throw;
            }
        }
    }

    private (ProcessTermSequence506 Payload, HamtViewNode Root) BuildSharedHamt526(
        PreparedOwnedMap517 prepared,
        Term[] headChildren,
        List<ProcessTermSequence506> allocated,
        OwnedMapRow517[] rows
    )
    {
        var stored = new Term?[prepared.Nodes.Length];
        var payloads = new ProcessTermSequence506[prepared.Nodes.Length];
        var views = new HamtViewNode[prepared.Nodes.Length];
        OwnedMapNodePlan517 head = prepared.Nodes[0];
        if (!head.Head || headChildren.Length != head.Children.Length)
            throw new InvalidOperationException(OwnedMapPayloadNames517.InvalidShape);
        for (int index = 0; index < head.Children.Length; index++)
            stored[head.Children[index]] = headChildren[index];
        for (int index = 1; index < prepared.Nodes.Length; index++)
        {
            Term term = stored[index] ?? throw new InvalidOperationException(OwnedMapPayloadNames517.InvalidShape);
            if (!IsOwnedLocal(term) || Storage(term) is not ProcessTermSequence506 payload)
                throw new InvalidOperationException(OwnedMapPayloadNames517.InvalidShape);
            OwnedMapNodePlan517 shape = prepared.Nodes[index];
            OwnedMapHeaderKind517 kind = shape.Row >= 0 ? OwnedMapHeaderKind517.HamtLeafCons
                : shape.Collision ? OwnedMapHeaderKind517.HamtCollision : OwnedMapHeaderKind517.HamtBitmap;
            OwnedMapHeader517 header = ReadMapHeader517(payload);
            if (header.Kind != kind || header.PairCount != prepared.NodePairCounts526[index]
                || header.Bitmap != shape.Bitmap || payload.Count != (shape.Row >= 0 ? 2 : shape.Children.Length)
                || kind == OwnedMapHeaderKind517.HamtLeafCons && term is not Cons
                || kind == OwnedMapHeaderKind517.HamtCollision && term is not TupleTerm
                || kind == OwnedMapHeaderKind517.HamtBitmap && term is not HamtBitmapTerm429)
                throw new InvalidOperationException(OwnedMapPayloadNames517.InvalidShape);
            payloads[index] = payload;
            if (shape.Row >= 0)
                rows[shape.Row] = new(
                    payload,
                    0,
                    payload,
                    1
                );
            else
                for (int child = 0; child < shape.Children.Length; child++)
                    stored[shape.Children[child]] = payload[child];
        }
        payloads[0] = AllocateMap517(headChildren,
            new(
                OwnedMapHeaderKind517.HamtHead,
                headChildren.Length,
                prepared.Count,
                head.Bitmap
            ), allocated);
        for (int index = prepared.Nodes.Length - 1; index >= 0; index--)
        {
            var children = Array.AsReadOnly(prepared.Nodes[index].Children.Select(child => views[child]).ToArray());
            views[index] = new HamtViewNode(new OwnedMapHamtView517(
                this,
                payloads[index],
                children,
                stored[index]
            ));
        }

        return (payloads[0], views[0]);
    }
}
