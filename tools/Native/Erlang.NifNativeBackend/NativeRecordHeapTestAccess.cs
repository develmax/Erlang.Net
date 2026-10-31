using System.Reflection;
using System.Numerics;

namespace Erlang.NifNativeBackend;

internal static class NativeRecordHeapTestAccess
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

    internal static object? Storage(NativeRecordTerm value) => typeof(NativeRecordTerm)
        .GetProperty(NativeRecordHeapCaseNames.Storage, Fields)!.GetValue(value);

    internal static NativeRecordTerm Input()
    {
        Atom a = Term.A(NativeRecordHeapCaseNames.FieldA);
        Atom z = Term.A(NativeRecordHeapCaseNames.FieldZ);

        return new NativeRecordTerm(
            Term.A(NativeRecordHeapCaseNames.Module),
            Term.A(NativeRecordHeapCaseNames.Record),
            true,
            [new(z, new Integer(BigInteger.One << 200)), new(a, new BitString(new byte[] { 1, 2, 3 }))]
        );
    }

    internal static ProcessTermSequence506 Inspect(ProcessTermHeap506 heap, NativeRecordTerm value)
    {
        lock (heap.Gate)
        {
            Require(Storage(value) is ProcessTermSequence506);
            var block = (ProcessTermSequence506)Storage(value)!;
            Require(ReferenceEquals(block.Owner, heap) && block.AllocationWords526 == 4 && block.Count == 2
                && block.HeaderWords526 == 1 && block.RawPayloadWords556 == 1
                && block.Slab.Cells.ReadHeader526(block.AllocationOffset526) == NativeRecordHeapCaseNames.TwoFieldHeader);
            TupleTerm definition = heap.ReadRecordDefinition(block);
            Require(definition.Items.Count == 7 && definition.Items[0] is TupleTerm { Items.Count: 2 }
                && definition.Items[1] is Integer { Value.Sign: >= 0 } && definition.Items[2].Equals(value.Module)
                && definition.Items[3].Equals(value.Name) && definition.Items[4].Equals(Term.A(ErlangBooleanAtoms.True)));
            var definitionBlock = (ProcessTermSequence506)typeof(ProcessTermHeap506)
                .GetMethod(NativeRecordHeapCaseNames.HeapStorage, BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, [definition])!;
            Require(ReferenceEquals(definitionBlock.Owner, heap) && definitionBlock.AllocationWords526 == 8);
            nuint actual = block.Slab.Cells.ReadNumeric556(block.AllocationOffset526 + 1);
            nuint expected = (nuint)heap.CaptureRawAddress571(definitionBlock, 0, 1).Address571 | 2;
            Require(actual == expected);

            return block;
        }
    }

    internal static IReadOnlyList<ProcessTermSequence506> ExistingRecords(ProcessTermHeap506 heap)
    {
        lock (heap.Gate)
        {
            var definitions = (Dictionary<long, TupleTerm>)typeof(ProcessTermHeap506).GetField(NativeRecordHeapCaseNames.Definitions, Fields)!.GetValue(heap)!;
            var blocks = (Dictionary<long, ProcessTermSequence506>)typeof(ProcessTermHeap506).GetField(NativeRecordHeapCaseNames.Blocks, Fields)!.GetValue(heap)!;

            return definitions.Keys.Select(id => blocks[id]).ToArray();
        }
    }

    internal static void RequireStale(Action read)
    {
        bool refused = false;
        try
        {
            read();
        }
        catch (ObjectDisposedException)
        {
            refused = true;
        }
        Require(refused);
    }

    internal static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(NativeRecordHeapCaseNames.Failure);
    }
}
