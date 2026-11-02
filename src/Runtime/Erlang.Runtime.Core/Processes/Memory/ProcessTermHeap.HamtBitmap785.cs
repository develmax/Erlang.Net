namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    internal Term CreateHamtBitmap785(
        PreparedHamtBitmap785 prepared,
        ProcessTermConstruction514 construction,
        Term[] copiedChildren,
        PreparedHamtBitmapUse785 admission
    )
    {
        if (!admission.IsFor(prepared) || !ReferenceEquals(prepared.Runtime, runtime))
            throw new InvalidOperationException(OwnedMapPayloadNames517.PreparationRequired);
        Term[] cells = (Term[])copiedChildren.Clone();
        if (cells.Length != prepared.Children.Length)
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
                        OwnedMapHeaderKind517.HamtBitmap,
                        cells.Length,
                        prepared.PairCount,
                        prepared.Source.Bitmap
                    ),
                    allocated);
                var value = new HamtBitmapTerm429(
                    prepared.Source.Domain,
                    prepared.Source.Ownership,
                    prepared.Source.Bitmap,
                    payload
                );
                construction.Publish(value);

                return value;
            }
            catch
            {
                foreach (ProcessTermSequence506 payload in allocated)
                {
                    ReleaseAllocation519(payload.Slab, payload.AllocationOffset526, payload.AllocationWords526);
                    blocks.Remove(payload.Identity);
                    ReleaseLiteralBlock801(payload.Identity);
                    oldBlocks508.Remove(payload.Identity);
                    mapHeaders517.Remove(payload.Identity);
                }
                throw;
            }
        }
    }
}
