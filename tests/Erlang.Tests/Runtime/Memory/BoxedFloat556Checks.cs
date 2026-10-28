using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.BoxedFloat556Names;

namespace Erlang.Tests;

internal static class BoxedFloat556Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static Task PhysicalGetterAndGc() => RunWithCleanup(async fixture =>
    {
        ProcessRuntime runtime = fixture.Runtime;
        object heap = fixture.CreateHeap();
        IDisposable roots = fixture.EnterRoots(heap);
        int slot = (int)Call(roots, Reserve)!;
        long[] bits = [0, long.MinValue, 0x3ff8000000000000, 1, unchecked((long)0x8000000000000001UL), 0x7fefffffffffffff];
        foreach (long valueBits in bits)
        {
            Call(
                roots,
                Set,
                slot,
                new FloatTerm(BitConverter.Int64BitsToDouble(valueBits))
            );
            var owned = (FloatTerm)Call(roots, Read, slot)!;
            Check(BitConverter.DoubleToInt64Bits(owned.Value) == valueBits);
            Call(heap, Collect);
            Check(Words(heap, Physical) == 2 && Words(heap, Headers) == 1
                && Words(heap, Numeric) == 1 && Words(heap, Handles) == 0);
            object storage = Get(owned, Storage);
            Check((int)Get(storage, Count) == 0);
            object cells = Get(Get(storage, Slab), Cells);
            int offset = (int)Get(storage, Offset);
            const long replacementBits = 0x4004000000000000;
            lock (Get(heap, HeapGate))
                Call(
                    cells,
                    WriteNumeric,
                    new nuint[] { unchecked((nuint)replacementBits) },
                    offset + 1
                );
            Check(BitConverter.DoubleToInt64Bits(owned.Value) == replacementBits);
            lock (Get(heap, HeapGate))
                Call(
                    cells,
                    WriteNumeric,
                    new nuint[] { unchecked((nuint)valueBits) },
                    offset + 1
                );
            nuint header = (nuint)Call(cells, ReadHeader, offset)!;
            lock (Get(heap, HeapGate))
                Call(
                    cells,
                    WriteHeaders,
                    new nuint[] { 0 },
                    offset
                );
            Expect<InvalidOperationException>(() => _ = owned.Value);
            lock (Get(heap, HeapGate))
                Call(
                    cells,
                    WriteHeaders,
                    new nuint[] { header },
                    offset
                );
            var exported = (FloatTerm)Call(
                heap,
                Export,
                roots,
                owned
            )!;
            Call(roots, Clear, slot);
            Call(heap, Collect);
            Check(Words(heap, Physical) == 0 && Words(heap, Numeric) == 0);
            Expect<ObjectDisposedException>(() => _ = owned.Value);
            Check(BitConverter.DoubleToInt64Bits(exported.Value) == valueBits);
        }
        roots.Dispose();
        await ((Task)Call(heap, Retire)!).WaitAsync(TimeSpan.FromSeconds(DrainTimeoutSeconds));
    });

    internal static Task AdoptionAndRootOnly() => RunWithCleanup(async fixture =>
    {
        ProcessRuntime runtime = fixture.Runtime;
        object heap = fixture.CreateHeap();
        IDisposable roots = fixture.EnterRoots(heap);
        var portable = new FloatTerm(1.5);
        Check(ReferenceEquals(Call(roots, RetainLocal, portable), portable));
        Check(Words(heap, Physical) == 0);
        int slot = (int)Call(roots, Reserve)!;
        var graph = (TupleTerm)Call(
            roots,
            Adopt,
            slot,
            Term.Tuple(portable, portable, new FloatTerm(1.5))
        )!;
        Check(ReferenceEquals(graph.Items[0], graph.Items[1])
            && !ReferenceEquals(graph.Items[0], graph.Items[2])
            && !ReferenceEquals(graph.Items[0], portable));
        Check(Words(heap, Physical) == 8 && Words(heap, Numeric) == 2
            && Words(heap, Handles) == 3);
        Call(heap, Collect);
        Check(((FloatTerm)graph.Items[0]).Value == portable.Value);
        // This exercises the exact CWT policy seam only. Genuine compiled
        // literal-image admission remains a required affected regression.
        var marked = new FloatTerm(0.5);
        Call(runtime, MarkLiterals, (object)new Term[] { marked });
        long previous = Words(heap, Physical);
        int literalSlot = (int)Call(roots, Reserve)!;
        Check(ReferenceEquals(Call(
            roots,
            Adopt,
            literalSlot,
            marked
        ), marked));
        Check(Words(heap, Physical) == previous);
        var equalButDynamic = new FloatTerm(0.5);
        int dynamicSlot = (int)Call(roots, Reserve)!;
        Check(!ReferenceEquals(Call(
            roots,
            Adopt,
            dynamicSlot,
            equalButDynamic
        ), equalButDynamic));
        Check(Words(heap, Physical) == previous + 2);
        roots.Dispose();
        await ((Task)Call(heap, Retire)!).WaitAsync(TimeSpan.FromSeconds(DrainTimeoutSeconds));
    });

    internal static Task ForeignAndHeldRetirement() => RunWithCleanup(async fixture =>
    {
        ProcessRuntime runtime = fixture.Runtime;
        object source = fixture.CreateHeap();
        object destination = fixture.CreateHeap();
        IDisposable sourceRoots = fixture.EnterRoots(source);
        IDisposable targetRoots = fixture.EnterRoots(destination);
        int sourceSlot = (int)Call(sourceRoots, Reserve)!;
        int targetSlot = (int)Call(targetRoots, Reserve)!;
        Call(
            sourceRoots,
            Set,
            sourceSlot,
            new FloatTerm(3.5)
        );
        var owned = (FloatTerm)Call(sourceRoots, Read, sourceSlot)!;
        Call(
            source,
            CollectGeneration,
            false,
            ushort.MaxValue
        );
        Call(
            source,
            CollectGeneration,
            false,
            ushort.MaxValue
        );
        Call(sourceRoots, Clear, sourceSlot);
        Call(
            source,
            CollectGeneration,
            false,
            ushort.MaxValue
        );
        Check(Words(source, Physical) == 2 && owned.Value == 3.5);
        Call(
            sourceRoots,
            Set,
            sourceSlot,
            owned
        );
        Expect<InvalidOperationException>(() => Call(
            targetRoots,
            Adopt,
            targetSlot,
            owned
        ));
        Check(Words(destination, Physical) == 0);
        var portable = (FloatTerm)Call(
            source,
            Export,
            sourceRoots,
            owned
        )!;
        Call(
            targetRoots,
            Set,
            targetSlot,
            portable
        );
        var copied = (FloatTerm)Call(targetRoots, Read, targetSlot)!;
        Check(copied.Value == owned.Value && !ReferenceEquals(copied, owned));
        Task retiring = (Task)Call(source, Retire)!;
        Check(!retiring.IsCompleted);
        Call(source, Collect);
        Check(owned.Value == 3.5);
        sourceRoots.Dispose();
        await retiring.WaitAsync(TimeSpan.FromSeconds(DrainTimeoutSeconds));
        Expect<ObjectDisposedException>(() => _ = owned.Value);
        Check(copied.Value == 3.5 && portable.Value == 3.5);
        targetRoots.Dispose();
        await ((Task)Call(destination, Retire)!).WaitAsync(TimeSpan.FromSeconds(DrainTimeoutSeconds));
    });

    // The finally path owns every heap independently of the assertion path.
    // Keep the original assertion first; report each cleanup failure as well.
    private static async Task RunWithCleanup(Func<HeapFixture, Task> body)
    {
        var fixture = new HeapFixture(OwnedGuest506RuntimeFactory.Create());
        var failures = new List<Exception>();
        try
        {
            await body(fixture);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            await fixture.Drain(failures);
        }
        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(AssertionAndCleanupFailure, failures);
    }

    private sealed class HeapFixture(ProcessRuntime runtime)
    {
        private readonly List<object> heaps = new();
        private readonly List<IDisposable> frames = new();
        internal ProcessRuntime Runtime => runtime;

        internal object CreateHeap()
        {
            heaps.EnsureCapacity(checked(heaps.Count + 1));
            object heap = NewHeap(runtime);
            heaps.Add(heap);

            return heap;
        }

        internal IDisposable EnterRoots(object heap)
        {
            frames.EnsureCapacity(checked(frames.Count + 1));
            var frame = (IDisposable)Call(heap, Enter)!;
            frames.Add(frame);

            return frame;
        }

        internal async Task Drain(List<Exception> failures)
        {
            for (int index = frames.Count - 1; index >= 0; index--)
            {
                try
                {
                    frames[index].Dispose();
                }
                catch (Exception error)
                {
                    failures.Add(new InvalidOperationException(FrameCleanupFailure, error));
                }
            }
            // A failed frame or heap does not skip retirement of another heap.
            foreach (object heap in heaps)
            {
                try
                {
                    await ((Task)Call(heap, Retire)!).WaitAsync(TimeSpan.FromSeconds(DrainTimeoutSeconds));
                }
                catch (Exception error)
                {
                    failures.Add(new InvalidOperationException(HeapCleanupFailure, error));
                }
            }
            try
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(DrainTimeoutSeconds));
            }
            catch (Exception error)
            {
                failures.Add(new InvalidOperationException(RuntimeCleanupFailure, error));
            }
        }
    }

    private static object NewHeap(ProcessRuntime runtime)
        => Activator.CreateInstance(
            typeof(ProcessRuntime).Assembly.GetType(HeapType, true)!,
            Members,
            null,
            [runtime],
            null
        )!;

    private static long Words(object heap, string property) => (long)Get(Get(heap, Snapshot), property);

    private static object Get(object target, string property)
        => target.GetType().GetProperty(property, Members)!.GetValue(target)!;

    private static object? Call(object target, string method, params object?[] arguments)
    {
        try
        {
            return target.GetType().GetMethod(method, Members)!.Invoke(target, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static void Expect<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException(Failure);
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
