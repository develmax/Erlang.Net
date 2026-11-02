using static Erlang.Tests.AllocatorFirstFit519Names;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

internal static class AllocatorFirstFit519Checks
{
    private static readonly Assembly RuntimeAssembly = typeof(ProcessRuntime).Assembly;
    private static readonly Type SlabType = RuntimeAssembly.GetType(SlabName, true)!;
    private static readonly Type HeapType = RuntimeAssembly.GetType(HeapName, true)!;

    internal static Task MixedFirstFit()
    {
        const int words = 257;
        object slab = Activator.CreateInstance(
            SlabType,
            Members,
            null,
            new object[] { words },
            null
        )!;
        using var slabLifetime = (IDisposable)slab;
        var occupied = new bool[words];
        var live = new List<(int Offset, int Length)>();
        var random = new Random(519);
        for (int step = 0; step < 4000; step++)
        {
            if (live.Count != 0 && random.Next(4) == 0)
            {
                int selected = random.Next(live.Count);
                var old = live[selected];
                Call(
                    slab,
                    Release,
                    old.Offset,
                    old.Length
                );
                Array.Fill(
                    occupied,
                    false,
                    old.Offset,
                    old.Length
                );
                live.RemoveAt(selected);
                continue;
            }
            int length = random.Next(1, 19);
            int expected = FirstFit(occupied, length);
            int actual = (int)Call(slab, Reserve, length)!;
            Check(actual == expected);
            if (actual < 0)
                continue;
            Array.Fill(
                occupied,
                true,
                actual,
                length
            );
            live.Add((actual, length));
        }

        return Task.CompletedTask;
    }

    internal static Task FragmentedLinearScan()
    {
        const int count = 4096;
        object slab = Activator.CreateInstance(
            SlabType,
            Members,
            null,
            new object[] { count * 2 + 4 },
            null
        )!;
        using var slabLifetime = (IDisposable)slab;
        Check((int)Call(slab, Reserve, 1)! == 0);
        Check((int)Call(slab, Reserve, 1)! == 1);
        Check((int)Call(slab, Reserve, 1)! == 2);
        Call(
            slab,
            Release,
            0,
            1
        );
        long before = (long)Property(slab, CellVisits)!;
        for (int index = 0; index < count; index++)
            Check((int)Call(slab, Reserve, 2)! == 3 + index * 2);
        long inspected = (long)Property(slab, CellVisits)! - before;
        Console.WriteLine(BitmapDiagnostic, inspected, count);
        Check(inspected <= count * 4L + 8);
        // Length-specific skip cannot hide the earlier single-word hole.
        Check((int)Call(slab, Reserve, 1)! == 0);

        return Task.CompletedTask;
    }

    internal static async Task HeapLinearScan()
    {
        const int count = 4096;
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        object heap = Activator.CreateInstance(
            HeapType,
            Members,
            null,
            new object[] { runtime },
            null
        )!;
        try
        {
            for (int index = 0; index < count; index++)
                Allocate(heap, 2);
            long visits = (long)Property(heap, SlabVisits)!;
            Console.WriteLine(SlabDiagnostic, visits, count);
            Check(visits <= count * 2L);
        }
        finally
        {
            await (Task)Call(heap, Retire)!;
        }
    }

    internal static async Task CollectionAndRollbackReuse()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        object heap = Activator.CreateInstance(
            HeapType,
            Members,
            null,
            new object[] { runtime },
            null
        )!;
        object frame = Call(heap, Enter)!;
        object? borrow = null;
        try
        {
            TupleTerm first = Allocate(heap, 2);
            TupleTerm middle = Allocate(heap, 3);
            Allocate(heap, 4);
            borrow = Call(
                heap,
                Borrow,
                frame,
                middle
            )!;
            Call(
                heap,
                Collect,
                false,
                ushort.MaxValue
            );
            TupleTerm reused = Allocate(heap, 2);
            Check(ReferenceEquals(BlockProperty(first, Slab), BlockProperty(reused, Slab)));
            Check((int)BlockProperty(reused, Offset)! == 0);
            TupleTerm larger = Allocate(heap, 4);
            Check((int)BlockProperty(larger, Offset)! == 5);
            FieldInfo identity = HeapType.GetField(Identity, Members)!;
            long prior = (long)identity.GetValue(heap)!;
            identity.SetValue(heap, long.MaxValue);
            bool failed = false;
            try
            {
                Allocate(heap, 2);
            }
            catch (OverflowException)
            {
                failed = true;
            }
            finally
            {
                identity.SetValue(heap, prior);
            }
            Check(failed);
            TupleTerm afterFailure = Allocate(heap, 2);
            Check((int)BlockProperty(afterFailure, Offset)! == 9);
            // Releasing every live block must invalidate hints for all lengths.
            ((IDisposable)borrow).Dispose();
            borrow = null;
            Call(
                heap,
                Collect,
                true,
                ushort.MaxValue
            );
            Check((int)BlockProperty(Allocate(heap, 11), Offset)! == 0);
        }
        finally
        {
            (borrow as IDisposable)?.Dispose();
            ((IDisposable)frame).Dispose();
            await (Task)Call(heap, Retire)!;
        }
    }

    private static TupleTerm Allocate(object heap, int physicalWords)
    {
        // Every measured block is a genuine tuple: one arity header plus its
        // immediate fields. Preserve the original physical first-fit requests.
        Check(physicalWords > TupleHeaderWords);
        object gate = Property(heap, HeapGate)!;
        lock (gate)
        {
            var children = Enumerable.Repeat<Term>(Term.I(PayloadValue), physicalWords - TupleHeaderWords).ToArray();
            var value = (TupleTerm)Call(heap, CreateTuple, new object[] { children })!;
            Check(value.Items.Count == physicalWords - TupleHeaderWords);
            Check((int)BlockProperty(value, HeaderWords)! == TupleHeaderWords);
            Check((int)BlockProperty(value, AllocationWords)! == physicalWords);
            foreach (Term child in value.Items) Check(child.Equals(Term.I(PayloadValue)));

            return value;
        }
    }

    private static object? BlockProperty(TupleTerm value, string name)
        => Property(Property(value, Storage)!, name);

    private static object? Property(object value, string name)
        => value.GetType().GetProperty(name, Members)!.GetValue(value);

    private static object? Call(object target, string name, params object[] args)
    {
        try
        {
            return target.GetType().GetMethod(name, Members)!.Invoke(target, args);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static int FirstFit(bool[] occupied, int length)
    {
        int run = 0;
        for (int index = 0; index < occupied.Length; index++)
        {
            run = occupied[index] ? 0 : run + 1;
            if (run == length)
                return index + 1 - length;
        }

        return -1;
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(nameof(AllocatorFirstFit519Checks));
    }
}
