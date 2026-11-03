using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.BoxedInteger559TestNames;

namespace Erlang.Tests;

internal static class BoxedInteger559Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static Task ImmediateChildrenImportAndAdoption() => RunWithCleanup(async fixture =>
    {
        object heap = fixture.CreateHeap();
        var failures = new List<Exception>();
        BigInteger[] values = [BigInteger.Zero, BigInteger.One, -BigInteger.One,
            -(BigInteger.One << 59), (BigInteger.One << 59) - 1];
        foreach (BigInteger value in values)
        {
            Term[] shapes =
            [
                Term.Tuple(new Integer(value), Term.Tuple(new Integer(value))),
                Term.List(new Integer(value)),
                new MapTerm(new KeyValuePair<Term, Term>[]
                {
                    new(new Integer(value), new Integer(value))
                })
            ];
            for (int shape = 0; shape < shapes.Length; shape++)
            {
                foreach (string route in new[] { Set, SetPrepared, Adopt })
                {
                    IDisposable? roots = null;
                    try
                    {
                        roots = fixture.EnterRoots(heap);
                        int slot = (int)Call(roots, Reserve)!;
                        Call(
                            roots,
                            route,
                            slot,
                            shapes[shape]
                        );
                        var actual = (Term)Call(roots, Read, slot)!;
                        Call(heap, Collect);
                        var portable = (Term)Call(
                            heap,
                            Export,
                            roots,
                            actual
                        )!;
                        Check(portable.Equals(shapes[shape]));
                        Check(Words(heap, Numeric) == 0);
                        Call(roots, Clear, slot);
                    }
                    catch (Exception error)
                    {
                        failures.Add(new InvalidOperationException(
                            TransferCaseFailure + route + TransferCaseSeparator + shape
                                + TransferCaseSeparator + value, error));
                    }
                    finally
                    {
                        try
                        {
                            roots?.Dispose();
                        }
                        catch (Exception error)
                        {
                            failures.Add(new InvalidOperationException(FrameCleanupFailure, error));
                        }
                        try
                        {
                            Call(heap, Collect);
                            Check(Words(heap, Physical) == 0);
                        }
                        catch (Exception error)
                        {
                            failures.Add(new InvalidOperationException(HeapCleanupFailure, error));
                        }
                    }
                }
            }
        }
        if (failures.Count != 0)
            throw new AggregateException(TransferCaseFailure, failures);
        await ((Task)Call(heap, Retire)!).WaitAsync(TimeSpan.FromSeconds(DrainTimeoutSeconds));
    });

    internal static Task PayloadAndCollection() => RunWithCleanup(async fixture =>
    {
        object heap = fixture.CreateHeap();
        IDisposable roots = fixture.EnterRoots(heap);
        int slot = (int)Call(roots, Reserve)!;
        (BigInteger Value, int Digits)[] cases =
        [
            (BigInteger.One << 59, 1),
            (-((BigInteger.One << 59) + 1), 1),
            ((BigInteger.One << 64) - 1, 1),
            (BigInteger.One << 64, 2),
            (-((BigInteger.One << 127) - 1), 2),
            (BigInteger.One << 128, 3),
            ((BigInteger.One << 1024) + (BigInteger.One << 64) + 7, 17)
        ];
        foreach ((BigInteger value, int digits) in cases)
        {
            Call(
                roots,
                Set,
                slot,
                new Integer(value)
            );
            var owned = (Integer)Call(roots, Read, slot)!;
            Check(owned.Value == value);
            Call(heap, Collect);
            Check(Words(heap, Physical) == digits + 1 && Words(heap, Headers) == 1
                && Words(heap, Numeric) == digits && Words(heap, Handles) == 0);
            object storage = Get(owned, Storage);
            Check((int)Get(storage, Count) == 0);
            object cells = Get(Get(storage, Slab), Cells);
            int offset = (int)Get(storage, Offset);
            nuint low;
            lock (Get(heap, HeapGate))
            {
                low = (nuint)Call(cells, ReadNumeric, offset + 1)!;
                Call(
                    cells,
                    WriteNumeric,
                    new nuint[] { low ^ MutationMask },
                    offset + 1
                );
            }
            BigInteger delta = new BigInteger((ulong)MutationMask);
            if ((low & MutationMask) != 0)
                delta = -delta;
            Check(owned.Value == value + (value.Sign < 0 ? -delta : delta));
            nuint header;
            lock (Get(heap, HeapGate))
            {
                Call(
                    cells,
                    WriteNumeric,
                    new nuint[] { low },
                    offset + 1
                );
                header = (nuint)Call(cells, ReadHeader, offset)!;
                Call(
                    cells,
                    WriteHeaders,
                    new nuint[] { 0 },
                    offset
                );
            }
            Expect<InvalidOperationException>(() => _ = owned.Value);
            lock (Get(heap, HeapGate))
                Call(
                    cells,
                    WriteHeaders,
                    new nuint[] { header },
                    offset
                );
            nuint high;
            lock (Get(heap, HeapGate))
            {
                high = (nuint)Call(cells, ReadNumeric, offset + digits)!;
                Call(
                    cells,
                    WriteNumeric,
                    new nuint[] { 0 },
                    offset + digits
                );
            }
            Expect<InvalidOperationException>(() => _ = owned.Value);
            lock (Get(heap, HeapGate))
                Call(
                    cells,
                    WriteNumeric,
                    new nuint[] { high },
                    offset + digits
                );
            var exported = (Integer)Call(
                heap,
                Export,
                roots,
                owned
            )!;
            Call(roots, Clear, slot);
            Call(heap, Collect);
            Check(Words(heap, Physical) == 0 && Words(heap, Numeric) == 0);
            Expect<ObjectDisposedException>(() => _ = owned.Value);
            Check(exported.Value == value);
        }
        roots.Dispose();
        await ((Task)Call(heap, Retire)!).WaitAsync(TimeSpan.FromSeconds(DrainTimeoutSeconds));
    });

    internal static Task ImmediateAliasLiteralAndLimits() => RunWithCleanup(async fixture =>
    {
        ProcessRuntime runtime = fixture.Runtime;
        object heap = fixture.CreateHeap();
        IDisposable roots = fixture.EnterRoots(heap);
        int slot = (int)Call(roots, Reserve)!;
        foreach (BigInteger value in new[] { -(BigInteger.One << 59), (BigInteger.One << 59) - 1, BigInteger.Zero })
        {
            var immediate = new Integer(value);
            Check(ReferenceEquals(Call(
                roots,
                Adopt,
                slot,
                immediate
            ), immediate));
            Check(Words(heap, Physical) == 0);
        }
        BigInteger magnitude = (BigInteger.One << 130) + 7;
        var portable = new Integer(magnitude);
        Check(ReferenceEquals(Call(roots, RetainLocal, portable), portable));
        Check(Words(heap, Physical) == 0);
        var graph = (TupleTerm)Call(
            roots,
            Adopt,
            slot,
            Term.Tuple(portable, portable, new Integer(magnitude))
        )!;
        Check(ReferenceEquals(graph.Items[0], graph.Items[1])
            && !ReferenceEquals(graph.Items[0], graph.Items[2])
            && !ReferenceEquals(graph.Items[0], portable));
        Check(Words(heap, Physical) == 12 && Words(heap, Numeric) == 6 && Words(heap, Handles) == 3);
        Call(heap, Collect);
        Check(((Integer)graph.Items[0]).Value == magnitude);
        // Policy seam only: this manual marker is not genuine BEAM admission.
        var marked = new Integer(-magnitude);
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
        int dynamicSlot = (int)Call(roots, Reserve)!;
        var dynamicEqual = new Integer(-magnitude);
        Check(!ReferenceEquals(Call(
            roots,
            Adopt,
            dynamicSlot,
            dynamicEqual
        ), dynamicEqual));
        Check(Words(heap, Physical) == previous + 4);
        // First forbidden native-digit arity. Check before any slab reservation;
        // no source-valid upper boundary is guessed from a host object size.
        int limitSlot = (int)Call(roots, Reserve)!;
        long before = Words(heap, Physical);
        var beyond = new Integer(BigInteger.One << MaximumBits);
        try
        {
            Call(
                roots,
                Adopt,
                limitSlot,
                beyond
            );
            throw new InvalidOperationException(Failure);
        }
        catch (ErlangException error)
        {
            Check(error.Reason.Equals(Term.A(ErlangErrorReasons.SystemLimit)));
        }
        Check(Words(heap, Physical) == before);
        IDisposable failedConstruction = fixture.EnterRoots(heap);
        object custody = Call(failedConstruction, BeginConstruction, portable)!;
        failedConstruction.Dispose();
        lock (Get(heap, HeapGate))
            Expect<ObjectDisposedException>(() => Call(
                heap,
                CreateInteger,
                magnitude,
                custody
            ));
        Check(Words(heap, Physical) == before && ((Integer)graph.Items[0]).Value == magnitude);
        roots.Dispose();
        await ((Task)Call(heap, Retire)!).WaitAsync(TimeSpan.FromSeconds(DrainTimeoutSeconds));
    });

    internal static Task ForeignAndHeldRetirement() => RunWithCleanup(async fixture =>
    {
        object source = fixture.CreateHeap();
        object destination = fixture.CreateHeap();
        IDisposable sourceRoots = fixture.EnterRoots(source);
        IDisposable targetRoots = fixture.EnterRoots(destination);
        int sourceSlot = (int)Call(sourceRoots, Reserve)!;
        int targetSlot = (int)Call(targetRoots, Reserve)!;
        BigInteger value = -(BigInteger.One << 130) - 7;
        Call(
            sourceRoots,
            Set,
            sourceSlot,
            new Integer(value)
        );
        var owned = (Integer)Call(sourceRoots, Read, sourceSlot)!;
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
        Check(Words(source, Physical) == 4 && owned.Value == value);
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
        var portable = (Integer)Call(
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
        var copied = (Integer)Call(targetRoots, Read, targetSlot)!;
        Check(copied.Value == value && !ReferenceEquals(copied, owned));
        Task retiring = (Task)Call(source, Retire)!;
        Check(!retiring.IsCompleted);
        Call(source, Collect);
        Check(owned.Value == value);
        sourceRoots.Dispose();
        await retiring.WaitAsync(TimeSpan.FromSeconds(DrainTimeoutSeconds));
        Expect<ObjectDisposedException>(() => _ = owned.Value);
        Check(copied.Value == value && portable.Value == value);
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
