using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using static Erlang.Tests.NativeRawAddress571TestNames;

namespace Erlang.Tests;

internal static class NativeRawAddress571Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static Task LiveCellsAndRanges() => RunWithCleanup(fixture =>
    {
        object heap = fixture.CreateHeap();
        IDisposable roots = fixture.EnterRoots(heap);
        int numberSlot = Slot(roots), tupleSlot = Slot(roots);
        Call(
            roots,
            Set,
            numberSlot,
            new FloatTerm(6.5)
        );
        var number = (FloatTerm)Call(roots, Read, numberSlot)!;
        object storage = Get(number, Storage);
        long occupied = Physical(heap);
        Check(occupied == 2 && number.Value == 6.5);
        object capability;
        lock (Get(heap, Gate))
        {
            capability = Call(
                heap,
                Capture,
                storage,
                0,
                2
            )!;
            nint address = (nint)Get(capability, Address);
            object payloadOnly = Call(
                heap,
                Capture,
                storage,
                1,
                1
            )!;
            Check((nint)Get(payloadOnly, Address) == address + sizeof(ulong));
            Check(Marshal.ReadInt64(address + sizeof(ulong)) == BitConverter.DoubleToInt64Bits(6.5));
            // The pointer names the genuine existing numeric cell. Changing it
            // changes the existing FloatTerm getter, not an accounting mirror.
            Marshal.WriteInt64(address + sizeof(ulong), BitConverter.DoubleToInt64Bits(7.75));
            Check(number.Value == 7.75);
            Marshal.WriteInt64(address + sizeof(ulong), BitConverter.DoubleToInt64Bits(6.5));
            object same = Call(
                heap,
                Capture,
                storage,
                0,
                2
            )!;
            Check((nint)Get(same, Address) == address);
            Expect<ArgumentOutOfRangeException>(() => Call(
                heap,
                Capture,
                storage,
                -1,
                1
            ));
            Expect<ArgumentOutOfRangeException>(() => Call(
                heap,
                Capture,
                storage,
                0,
                0
            ));
            Expect<ArgumentOutOfRangeException>(() => Call(
                heap,
                Capture,
                storage,
                2,
                1
            ));
            Expect<ArgumentOutOfRangeException>(() => Call(
                heap,
                Capture,
                storage,
                1,
                int.MaxValue
            ));
        }
        Expect<InvalidOperationException>(() => Get(capability, Address));
        Expect<InvalidOperationException>(() => Call(
            heap,
            Capture,
            storage,
            0,
            2
        ));
        Check(Physical(heap) == occupied && number.Value == 6.5);
        Call(
            roots,
            Set,
            tupleSlot,
            Term.Tuple(Term.I(1), Term.I(2))
        );
        var tuple = (TupleTerm)Call(roots, Read, tupleSlot)!;
        object tupleStorage = Get(tuple, TupleStorage);
        long withTuple = Physical(heap);
        lock (Get(heap, Gate))
        {
            _ = Get(Call(
                heap,
                Capture,
                tupleStorage,
                0,
                1
            )!, Address);
            // A genuine tuple header is raw, but the following words encode
            // GCHandles. Never expose those as raw native numeric/pointer data.
            Expect<InvalidOperationException>(() => Call(
                heap,
                Capture,
                tupleStorage,
                0,
                3
            ));
            Expect<InvalidOperationException>(() => Call(
                heap,
                Capture,
                tupleStorage,
                1,
                1
            ));
        }
        Check(Physical(heap) == withTuple && tuple.Items[0].Equals(Term.I(1)));

        return Task.CompletedTask;
    });

    internal static Task ForeignStaleAndRetirement() => RunWithCleanup(async fixture =>
    {
        object heap = fixture.CreateHeap(), foreignHeap = fixture.CreateHeap();
        IDisposable roots = fixture.EnterRoots(heap), foreignRoots = fixture.EnterRoots(foreignHeap);
        int slot = Slot(roots), foreignSlot = Slot(foreignRoots);
        Call(
            roots,
            Set,
            slot,
            new FloatTerm(9.5)
        );
        var number = (FloatTerm)Call(roots, Read, slot)!;
        object storage = Get(number, Storage);
        object stale;
        lock (Get(heap, Gate))
            stale = Call(
                heap,
                Capture,
                storage,
                0,
                2
            )!;
        lock (Get(foreignHeap, Gate))
            Expect<InvalidOperationException>(() => Call(
                foreignHeap,
                Capture,
                storage,
                0,
                2
            ));
        Check(Physical(foreignHeap) == 0 && number.Value == 9.5);
        Call(roots, Clear, slot);
        Call(heap, Collect);
        lock (Get(heap, Gate))
        {
            Expect<ObjectDisposedException>(() => Get(stale, Address));
            Expect<ObjectDisposedException>(() => Call(
                heap,
                Capture,
                storage,
                0,
                2
            ));
        }
        Check(Physical(heap) == 0);
        // Reuse may occupy the same slab range, but cannot revive old identity.
        Call(
            roots,
            Set,
            slot,
            new FloatTerm(10.5)
        );
        var replacement = (FloatTerm)Call(roots, Read, slot)!;
        object live;
        lock (Get(heap, Gate))
        {
            Expect<ObjectDisposedException>(() => Get(stale, Address));
            live = Call(
                heap,
                Capture,
                Get(replacement, Storage),
                0,
                2
            )!;
        }
        Task retiring = (Task)Call(heap, Retire)!;
        Check(!retiring.IsCompleted && replacement.Value == 10.5);
        lock (Get(heap, Gate))
            Check(Marshal.ReadInt64((nint)Get(live, Address) + sizeof(ulong)) == BitConverter.DoubleToInt64Bits(10.5));
        roots.Dispose();
        await retiring.WaitAsync(TimeSpan.FromSeconds(DrainTimeoutSeconds));
        lock (Get(heap, Gate))
            Expect<ObjectDisposedException>(() => Get(live, Address));
        Check(Physical(heap) == 0);
        Call(
            foreignRoots,
            Set,
            foreignSlot,
            new FloatTerm(11.5)
        );
        var independent = (FloatTerm)Call(foreignRoots, Read, foreignSlot)!;
        Check(independent.Value == 11.5 && Physical(foreignHeap) == 2);
    });

    private static int Slot(IDisposable frame) => (int)Call(frame, Reserve)!;

    private static long Physical(object heap) => (long)Get(Get(heap, Snapshot), Occupied);

    private static object Get(object target, string name)
    {
        try
        {
            return target.GetType().GetProperty(name, Members)!.GetValue(target)!;
        }
        catch (TargetInvocationException error) when (error.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

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

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }

    private static async Task RunWithCleanup(Func<Fixture, Task> body)
    {
        var fixture = new Fixture();
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
            throw new AggregateException(CleanupFailure, failures);
    }

    private sealed class Fixture
    {
        private readonly ProcessRuntime runtime = OwnedGuest506RuntimeFactory.Create();
        private readonly List<object> heaps = new();
        private readonly List<IDisposable> roots = new();

        internal object CreateHeap()
        {
            heaps.EnsureCapacity(checked(heaps.Count + 1));
            object heap = Activator.CreateInstance(
                typeof(ProcessRuntime).Assembly.GetType(HeapType, true)!,
                Members,
                null,
                [runtime],
                null
            )!;
            heaps.Add(heap);

            return heap;
        }

        internal IDisposable EnterRoots(object heap)
        {
            roots.EnsureCapacity(checked(roots.Count + 1));
            var frame = (IDisposable)Call(heap, Enter)!;
            roots.Add(frame);

            return frame;
        }

        internal async Task Drain(List<Exception> failures)
        {
            for (int index = roots.Count - 1; index >= 0; index--)
            {
                try
                {
                    roots[index].Dispose();
                }
                catch (Exception error)
                {
                    failures.Add(new InvalidOperationException(FrameCleanupFailure, error));
                }
            }
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
}
