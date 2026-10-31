using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Erlang.NifNativeBackend;

internal static class NativeWritableTestAccess
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;

    internal static ProcessTermHeap506 ExistingHeap(ProcessContext context)
    {
        object? value = typeof(ProcessContext).GetField(NativeWritableCaseNames.Heap, Members)!.GetValue(context);
        Require(value is ProcessTermHeap506);

        return (ProcessTermHeap506)value!;
    }

    internal static NativeWritableObservation Capture(ProcessContext context, bool collect)
    {
        ProcessTermHeap506 heap = ExistingHeap(context);
        lock (heap.Gate)
        {
            if (collect) _ = heap.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold);
            var descriptors = (IDictionary)typeof(ProcessTermHeap506).GetField(NativeWritableCaseNames.Descriptors, Members)!.GetValue(heap)!;
            var nodes = (IDictionary)typeof(ProcessTermHeap506).GetField(NativeWritableCaseNames.Nodes, Members)!.GetValue(heap)!;
            var addresses = (IDictionary)typeof(ProcessTermHeap506).GetField(NativeWritableCaseNames.Addresses, Members)!.GetValue(heap)!;
            var headers = new List<NativeWritableHeaderObservation>();
            long physicalWords = 0;
            foreach (ResourceBinaryDescriptor577 descriptor in descriptors.Values)
            {
                if (descriptor.BinRef.OrdinaryBinary is null) continue;
                ProcessTermSequence506 block = descriptor.Block;
                int offset = block.AllocationOffset526;
                physicalWords = checked(physicalWords + block.AllocationWords526);
                Require(block.AllocationWords526 == 5 && block.Slab.Cells.ReadHeader526(offset) == 0x124);
                headers.Add(new(
                    block.Identity,
                    descriptor.BinRef.Block.Identity,
                    block.Slab.Cells.ReadNumeric556(offset + 1),
                    block.Slab.Cells.ReadNumeric556(offset + 2),
                    block.Slab.Cells.ReadNumeric556(offset + 3)
                ));
            }
            var owners = new List<NativeWritableOwnerObservation>();
            foreach (NativeOffheapHeader576 node in nodes.Values)
            {
                if (node.OrdinaryBinary is not { } owner) continue;
                Require(node.Block.AllocationWords526 == 3 && node.Block.Slab.Cells.ReadHeader526(node.Block.AllocationOffset526) == 0xA8);
                physicalWords = checked(physicalWords + node.Block.AllocationWords526);
                nint pointer = owner.BinaryPointer;
                Require(node.Block.Slab.Cells.ReadNumeric556(node.Block.AllocationOffset526 + 1) == (nuint)pointer);
                owners.Add(new(
                    owner,
                    node.Block.Identity,
                    pointer + 32,
                    (nuint)Marshal.ReadInt64(pointer, 0),
                    (nuint)Marshal.ReadInt64(pointer, 8),
                    Marshal.ReadInt64(pointer, 16),
                    Marshal.ReadInt64(pointer, 24)
                ));
            }

            IReadOnlyList<long> Walk(string field)
            {
                nint cursor = (nint)typeof(ProcessTermHeap506).GetField(field, Members)!.GetValue(heap)!;
                var result = new List<long>();
                var seen = new HashSet<nint>();
                while (cursor != 0)
                {
                    Require(seen.Add(cursor) && addresses.Contains(cursor));
                    var node = (NativeOffheapHeader576)addresses[cursor]!;
                    result.Add(node.Block.Identity);
                    cursor = (nint)node.Block.Slab.Cells.ReadNumeric556(node.Block.AllocationOffset526 + 2);
                }

                return result;
            }

            return new(
                owners,
                headers.OrderBy(header => header.Identity).ToArray(),
                Walk(NativeWritableCaseNames.RegularHead),
                Walk(NativeWritableCaseNames.WritableHead),
                physicalWords
            );
        }
    }

    internal static void Dump(int phase, NativeWritableObservation snapshot)
    {
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
        {
            Phase = phase,
            snapshot.PhysicalWords,
            snapshot.Regular,
            snapshot.Writable,
            Owners = snapshot.Owners.Select(item => new
            {
                Owner = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(item.Owner),
                item.BinRef,
                Data = item.Data.ToString(),
                Flags = (ulong)item.Flags,
                Apparent = (ulong)item.Apparent,
                item.References,
                item.Capacity
            }),
            Headers = snapshot.Headers.Select(item => new
            {
                item.Identity,
                item.BinRef,
                Base = (ulong)item.Base,
                Start = (ulong)item.Start,
                End = (ulong)item.End
            })
        }));
    }

    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(NativeWritableCaseNames.Failure);
    }
}
