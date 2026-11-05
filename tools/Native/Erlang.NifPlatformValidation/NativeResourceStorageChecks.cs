using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using Erlang;

internal static class NativeResourceStorageChecks
{
    internal static unsafe Task Run()
    {
        int expectedData = IntPtr.Size == 4 ? 40 : 56;
        int expectedAtomics = IntPtr.Size == 4 ? 48 : 72;
#if DEBUG
        int expectedCounters = IntPtr.Size == 4 ? 48 : 72;
#else
        int expectedCounters = IntPtr.Size == 4 ? 48 : 64;
#endif
        Require(sizeof(NativeResourceMagicHeader527) == (IntPtr.Size == 4 ? 36 : 56));
        Require(Marshal.OffsetOf<AtomicsMagicHeader413>(nameof(AtomicsMagicHeader413.Data)).ToInt32() == expectedData);
        Require(Marshal.OffsetOf<CountersMagicHeader421>(nameof(CountersMagicHeader421.Data)).ToInt32() == expectedData);
        Require(sizeof(AtomicsMagicHeader413) == expectedAtomics && sizeof(CountersMagicHeader421) == expectedCounters);
        Require(NativeResourceMagic527.UserOffset527 == (IntPtr.Size == 4 ? 48 : 72));
        Require(MagicReference576Names.Header == ((nuint)2 << 6 | OrdinaryReference562Names.ReferenceSubtag));
        var atomics = new AtomicsArray413(2, true);
        try
        {
            using var lease = atomics.Acquire()!;
            Require(((nuint)Unsafe.AsPointer(ref atomics.Cell(0)) & 7) == 0);
            Interlocked.Exchange(ref atomics.Cell(0), long.MaxValue);
            Require(Interlocked.Increment(ref atomics.Cell(0)) == long.MinValue);
            Interlocked.Exchange(ref atomics.Cell(0), 0);
            Parallel.For(0, 4, _ =>
 {
     for (int i = 0; i < 5000; i++) Interlocked.Increment(ref atomics.Cell(0));
 });
            Require(Interlocked.Read(ref atomics.Cell(0)) == 20000 && atomics.Length == 2 && atomics.Signed);
        }
        finally
        {
            atomics.Close();
        }
        var counters = new CountersArray421(2, 2);
        try
        {
            using var lease = counters.Acquire()!;
            Require(((nuint)Unsafe.AsPointer(ref counters.Cell(0, 1)) & 7) == 0);
            Interlocked.Exchange(ref counters.Cell(0, 1), unchecked((long)0xfedcba9876543210UL));
            Require(Interlocked.Read(ref counters.Cell(0, 1)) == unchecked((long)0xfedcba9876543210UL));
            Require(counters.Length == 2 && counters.Lanes == 2);
        }
        finally
        {
            counters.Close();
        }
        Console.WriteLine(JsonSerializer.Serialize(new { Passed = true, Architecture = RuntimeInformation.ProcessArchitecture.ToString(), WordBytes = IntPtr.Size, MagicDataOffset = expectedData, AtomicsBytes = expectedAtomics, CountersBytes = expectedCounters, Atomic64ConcurrentIncrements = 20000, NativeResourceUserOffset = NativeResourceMagic527.UserOffset527 }));

        return Task.CompletedTask;
    }

    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(NifResourceValidationDiagnostics.Layout);
    }
}
