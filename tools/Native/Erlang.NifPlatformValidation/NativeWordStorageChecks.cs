using System.Numerics;
using System.Text.Json;
using System.Runtime.InteropServices;
using Erlang;

internal static class NativeWordStorageChecks
{
    internal static async Task Run()
    {
        using (var cells = new NativeTermCellBuffer526(4))
        {
            cells.WriteTerms526([Term.Tuple(new Integer(42))], 0);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (cells.RootedTerms526 != 1 || cells[0] is not TupleTerm)
                throw new InvalidOperationException(NifNativeDiagnostics.ManagedLayout);
            cells.WriteHeaders526([(nuint)uint.MaxValue], 0);
            if (cells.RootedTerms526 != 0 || cells.RawHeaders526 != 1 || cells.ReadHeader526(0) != uint.MaxValue)
                throw new InvalidOperationException(NifNativeDiagnostics.ManagedLayout);
            try
            {
                cells.ReadNumeric556(0);
                throw new InvalidOperationException(NifNativeDiagnostics.ManagedLayout);
            }
            catch (InvalidOperationException error) when (error.Message == CellStorage526Names.NotNumeric556) { }
            cells.Clear526(0, 4);
            if (cells.RawHeaders526 != 0 || cells.RootedTerms526 != 0)
                throw new InvalidOperationException(NifNativeDiagnostics.ManagedLayout);
        }
        await using var runtime = new ProcessRuntime();
        var heap = new ProcessTermHeap506(runtime);
        using var roots = heap.Enter();
        int slot = roots.Reserve();
        try
        {
            long[] bits = [0, long.MinValue, 1, unchecked((long)0x8000000000000001UL), 0x3ff8000000000000, 0x7fefffffffffffff];
            foreach (long value in bits)
            {
                roots.Set(slot, new FloatTerm(BitConverter.Int64BitsToDouble(value)));
                var owned = (FloatTerm)roots.Read(slot);
                heap.CollectGeneration508(false, 3);
                heap.Collect();
                if (BitConverter.DoubleToInt64Bits(owned.Value) != value)
                    throw new InvalidOperationException(NifNativeDiagnostics.ManagedLayout);
                var snapshot = heap.PhysicalWordSnapshot526;
                if (snapshot.LiveRawNumericWords556 != NativeWordLayout.DoubleWords)
                    throw new InvalidOperationException(NifNativeDiagnostics.ManagedLayout);
                roots.Clear(slot);
                heap.Collect();
                try
                {
                    _ = owned.Value;
                    throw new InvalidOperationException(NifNativeDiagnostics.ManagedLayout);
                }
                catch (ObjectDisposedException) { }
            }
            BigInteger[] integers = [(BigInteger.One << 27) - 1, BigInteger.One << 27, -(BigInteger.One << 27) - 1, BigInteger.One << 80, -(BigInteger.One << 130) + 1];
            foreach (BigInteger value in integers)
            {
                roots.Set(slot, Term.Tuple(new Integer(value), Term.List(new Integer(value))));
                Term owned = roots.Read(slot);
                heap.CollectGeneration508(false, 2);
                heap.Collect();
                if (!heap.Export(roots, owned).Equals(Term.Tuple(new Integer(value), Term.List(new Integer(value)))))
                    throw new InvalidOperationException(NifNativeDiagnostics.ManagedLayout);
                roots.Clear(slot);
                heap.Collect();
            }
            ReferenceTerm reference = runtime.MakeReference();
            roots.Set(slot, Term.Tuple(reference, new BitString(Enumerable.Range(0, 300).Select(i => (byte)i).ToArray())));
            Term graph = roots.Read(slot);
            heap.Collect();
            if (!heap.Export(roots, graph).Equals(Term.Tuple(reference, new BitString(Enumerable.Range(0, 300).Select(i => (byte)i).ToArray()))))
                throw new InvalidOperationException(NifNativeDiagnostics.ManagedLayout);
            roots.Clear(slot);
            heap.Collect();

            Term[] identifiers = [new Pid("remote@word", 0xfedcba9876543210UL, uint.MaxValue),
            new PortTerm("remote@word", ulong.MaxValue, uint.MaxValue),
            new ReferenceTerm("remote@word", new uint[] { uint.MaxValue, 0x87654321, 0xabcdef01 }, uint.MaxValue),
            new ReferenceTerm("remote@word", new uint[] { 0x12345678, uint.MaxValue, 0, 0xabcdef01 }, 17),
            new ReferenceTerm("remote@word", new uint[] { 1, 2, 3, 4, uint.MaxValue }, 23)];
            var receivingHeap = new ProcessTermHeap506(runtime);
            using var receivingRoots = receivingHeap.Enter();
            int receivingSlot = receivingRoots.Reserve();
            try
            {
                foreach (Term identifier in identifiers)
                {
                    roots.Set(slot, Term.Tuple(identifier, new FloatTerm(-1.5), new Integer(BigInteger.One << 100)));
                    Term sourceGraph = roots.Read(slot);
                    heap.CollectGeneration508(false, 2);
                    receivingRoots.Set(receivingSlot, heap.Export(roots, sourceGraph));
                    roots.Clear(slot);
                    heap.Collect();
                    receivingHeap.CollectGeneration508(false, 2);
                    receivingHeap.Collect();
                    Term expected = Term.Tuple(identifier, new FloatTerm(-1.5), new Integer(BigInteger.One << 100));
                    if (!receivingHeap.Export(receivingRoots, receivingRoots.Read(receivingSlot)).Equals(expected))
                        throw new InvalidOperationException(NifNativeDiagnostics.ManagedLayout);
                    receivingRoots.Clear(receivingSlot);
                    receivingHeap.Collect();
                }
            }
            finally
            {
                receivingRoots.Dispose();
                await receivingHeap.RetireAsync().WaitAsync(TimeSpan.FromSeconds(30));
            }
            var reclaimed = heap.PhysicalWordSnapshot526;
            if (reclaimed.Blocks != 0 || reclaimed.RootedTermHandles != 0 || reclaimed.LivePhysicalWords != 0)
                throw new InvalidOperationException(NifNativeDiagnostics.ManagedLayout);
            Console.WriteLine(JsonSerializer.Serialize(new { Passed = true, Architecture = RuntimeInformation.ProcessArchitecture.ToString(), WordBytes = IntPtr.Size, FloatCases = bits.Length, IntegerCases = integers.Length, MajorMinorGc = true, ExternalIdentifierCases = identifiers.Length, CrossHeapTransfer = true, CellRootsReclaimed = true }));
        }
        finally
        {
            roots.Dispose();
            await heap.RetireAsync().WaitAsync(TimeSpan.FromSeconds(30));
        }
    }
}

