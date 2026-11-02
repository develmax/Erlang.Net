namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    private WritableBinaryCollection PrepareWritableCollection(HashSet<long> marked, bool major)
    {
        var result = new WritableBinaryCollection();
        try
        {
            var live = new List<(OrdinaryBinaryBacking Owner, WritableBinaryBinding Binding, nuint Flags, nuint Apparent, int Capacity)>();
            int active = 0, inactive = 0;
            foreach (var node in magicReferences576.Values)
            {
                // Includes old-heap writable BinRefs even during a MINOR pass.
                if (!marked.Contains(node.Block.Identity) && (major || !oldBlocks508.Contains(node.Block.Identity))) continue;
                if (node.OrdinaryBinary is not { } owner || owner.WritableBinding is not { } binding
                    || !ReferenceEquals(binding.Heap, this) || !ReferenceEquals(binding.BinRef, node)) continue;
                var state = owner.WritableState;
                if ((state.Flags & NativeWritableBinaryLayout.Writable) == 0) continue;
                _ = RequireNativeOffheapHeader576(node.Block, NativeOffheapKind576.BinaryReference);
                live.Add((owner, binding, state.Flags, state.Apparent, state.Capacity));
                if ((state.Flags & NativeWritableBinaryLayout.ActiveWriter) != 0) active++;
                else if (state.Capacity - (int)state.Apparent >= NativeWritableBinaryLayout.PinSpareBytes) inactive++;
            }
            int leave = active != 0 ? 0 : inactive <= NativeWritableBinaryLayout.MuchLimit
                ? NativeWritableBinaryLayout.MuchPercent : inactive <= NativeWritableBinaryLayout.LeaveLimit
                    ? NativeWritableBinaryLayout.LeavePercent : 0;
            foreach (var item in live)
            {
                int size = item.Capacity;
                if ((item.Flags & NativeWritableBinaryLayout.ActiveWriter) == 0)
                {
                    int desired = checked((int)item.Apparent + (int)((long)item.Apparent * leave / 100));
                    // Pinned GC shrink uses STRICT new_size+8 < orig_size.
                    if ((long)desired + NativeWritableBinaryLayout.PinSpareBytes < size) size = desired;
                }
                result.Add(
                    item.Owner,
                    item.Binding,
                    item.Owner.PrepareMutation(size, item.Flags & ~NativeWritableBinaryLayout.ActiveWriter, item.Apparent)
                );
            }

            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }
}
