namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    // Internal physical primitive, not process_info admission. Walk the actual
    // MSO chains, including registered provisional headers: a metadata flag
    // must not hide an incorrectly exposed physical ownership boundary.
    internal NativeBinaryInfoRow822[] CaptureNativeBinaryInfo822()
    {
        runtime.RequireBinaryInfoCapture822();
        lock (Gate)
        {
            ObjectDisposedException.ThrowIf(closing, this);
            var visited = new HashSet<nint>();
            var rows = new List<NativeBinaryInfoRow822>();
            foreach (nint head in new[] { magicOffHeap576, writableOffHeap })
            {
                nint cursor = head;
                while (cursor != 0)
                {
                    if (!visited.Add(cursor) || !magicAddresses576.TryGetValue(cursor, out var node))
                        throw new InvalidOperationException(NativeBinaryInfo822Names.Invalid);
                    _ = RequireNativeOffheapHeader576(node.Block, node.Kind);
                    nint next = (nint)node.Block.Slab.Cells.ReadNumeric556(
                        node.Block.AllocationOffset526 + MagicReference576Names.NextWord
                    );
                    if (node.Kind == NativeOffheapKind576.BinaryReference)
                        rows.Add(node.OrdinaryBinary is { } ordinary
                            ? ordinary.ReadBinaryInfo822(node.Binary)
                            : node.Payload.ReadBinaryInfo822(node.Binary));
                    cursor = next;
                }
            }
            if (visited.Count != magicReferences576.Count || visited.Count != magicAddresses576.Count)
                throw new InvalidOperationException(NativeBinaryInfo822Names.Invalid);
            // OTP prepends a row for each BinRef, without backing deduplication.
            rows.Reverse();

            return rows.ToArray();
        }
    }
}
