using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.Allocation567TestNames;

namespace Erlang.Tests;

// Private actual-slab controls. These do not establish public ERTS heap totals.
internal static class Allocation567Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static Task ScalarGenerationsAndReclaim() => RunWithCleanup(fixture =>
    {
        object heap = fixture.CreateHeap(fixture.Runtime);
        IDisposable roots = fixture.EnterRoots(heap);
        int numberSlot = Slot(roots), integerSlot = Slot(roots), referenceSlot = Slot(roots);
        BigInteger magnitude = (BigInteger.One << 130) + 7;
        ReferenceTerm original = fixture.Runtime.MakeReference();
        Call(
            roots,
            Set,
            numberSlot,
            new FloatTerm(3.5)
        );
        Call(
            roots,
            Set,
            integerSlot,
            new Integer(magnitude)
        );
        Call(
            roots,
            Set,
            referenceSlot,
            original
        );
        var number = (FloatTerm)Call(roots, Read, numberSlot)!;
        var integer = (Integer)Call(roots, Read, integerSlot)!;
        var reference = (ReferenceTerm)Call(roots, Read, referenceSlot)!;
        Check(number.Value == 3.5 && integer.Value == magnitude && reference.Equals(original));
        // On the admitted 64-bit substrate: float header+double=2, integer
        // header+three real digit words=4, ordinary reference header+data=3.
        Check(IntPtr.Size == sizeof(ulong));
        Check(PhysicalWords(heap, Physical) == 9 && PhysicalWords(heap, Headers) == 3
            && PhysicalWords(heap, Numeric) == 6);
        AssertAllocation(
            heap,
            9,
            0,
            0,
            0
        );
        AssertPayload(
            heap,
            0,
            0,
            0,
            0
        );
        object first = Collection(heap, false);
        Check(Long(first, ReclaimedCollection) == 0 && Long(first, ReclaimedPayload) == 0);
        AssertAllocation(
            heap,
            0,
            9,
            0,
            0
        );
        Collection(heap, false);
        AssertAllocation(
            heap,
            0,
            0,
            9,
            0
        );
        Call(roots, Clear, numberSlot);
        Call(roots, Clear, integerSlot);
        Call(roots, Clear, referenceSlot);
        object minor = Collection(heap, false);
        Check(Long(minor, ReclaimedCollection) == 0 && Long(minor, ReclaimedPayload) == 0);
        AssertAllocation(
            heap,
            0,
            0,
            9,
            0
        );
        Check(number.Value == 3.5 && integer.Value == magnitude && reference.Equals(original));
        object major = Collection(heap, true);
        Check(Long(major, ReclaimedCollection) == 9 && Long(major, ReclaimedPayload) == 0);
        AssertAllocation(
            heap,
            0,
            0,
            0,
            9
        );
        AssertPayload(
            heap,
            0,
            0,
            0,
            0
        );
        Check(PhysicalWords(heap, Physical) == 0);
        Expect<ObjectDisposedException>(() => _ = number.Value);
        Expect<ObjectDisposedException>(() => _ = integer.Value);
        Expect<ObjectDisposedException>(() => _ = reference.Words);
        Check(original.Words.Count == 3);

        return Task.CompletedTask;
    });

    internal static Task SharedMapCustody() => RunWithCleanup(fixture =>
    {
        object heap = fixture.CreateHeap(fixture.Runtime);
        IDisposable producer = fixture.EnterRoots(heap), holder = fixture.EnterRoots(heap);
        int scalarSlot = Slot(producer), mapSlot = Slot(producer), heldSlot = Slot(holder);
        Call(
            producer,
            Set,
            scalarSlot,
            new FloatTerm(7.25)
        );
        var scalar = (FloatTerm)Call(producer, Read, scalarSlot)!;
        var source = new MapTerm(new KeyValuePair<Term, Term>[]
        {
            new(Term.A(First), scalar),
            new(Term.A(Second), scalar)
        });
        var map = (MapTerm)Call(
            producer,
            Adopt,
            mapSlot,
            source
        )!;
        Check(map.TryGet(Term.A(First), out Term? first) && ReferenceEquals(first, scalar));
        Check(map.TryGet(Term.A(Second), out Term? second) && ReferenceEquals(first, second));
        long occupied = PhysicalWords(heap, Physical);
        Check(occupied > 2 && Long(Get(heap, AllocationSnapshot), Nursery) == occupied);
        Call(
            holder,
            SetLocal,
            heldSlot,
            map
        );
        Check(PhysicalWords(heap, Physical) == occupied);
        Call(producer, Clear, scalarSlot);
        Call(producer, Clear, mapSlot);
        object held = Collection(heap, true);
        Check(Long(held, ReclaimedCollection) == 0 && scalar.Value == 7.25);
        Check(map.TryGet(Term.A(First), out Term? after) && ReferenceEquals(after, scalar));
        Check(PhysicalWords(heap, Physical) == occupied);
        Call(holder, Clear, heldSlot);
        object removed = Collection(heap, true);
        Check(Long(removed, ReclaimedCollection) == occupied);
        Check(Long(removed, ReclaimedPayload) > 0
            && Long(removed, ReclaimedPayload) < Long(removed, ReclaimedCollection));
        Check(PhysicalWords(heap, Physical) == 0);
        AssertAllocation(
            heap,
            0,
            0,
            0,
            occupied
        );
        Expect<ObjectDisposedException>(() => _ = scalar.Value);

        return Task.CompletedTask;
    });

    internal static Task FailedPublicationRollback() => RunWithCleanup(fixture =>
    {
        object heap = fixture.CreateHeap(fixture.Runtime);
        Term[] sources =
        [
            new FloatTerm(1.5),
            new Integer((BigInteger.One << 130) + 1),
            fixture.Runtime.MakeReference()
        ];
        foreach (Term source in sources)
        {
            IDisposable construction = fixture.EnterRoots(heap);
            object custody = Call(construction, BeginConstruction, source)!;
            construction.Dispose();
            lock (Get(heap, HeapGate))
            {
                if (source is FloatTerm number)
                    Expect<ObjectDisposedException>(() => Call(
                        heap,
                        CreateFloat,
                        number.Value,
                        custody
                    ));
                else if (source is Integer integer)
                    Expect<ObjectDisposedException>(() => Call(
                        heap,
                        CreateInteger,
                        integer.Value,
                        custody
                    ));
                else
                    Expect<ObjectDisposedException>(() => Call(
                        heap,
                        CreateReference,
                        source,
                        custody
                    ));
            }
            // The factories allocate actual cells, then publication fails and
            // rolls back. Failed construction is not a completed GC event.
            Check(PhysicalWords(heap, Physical) == 0);
            AssertAllocation(
                heap,
                0,
                0,
                0,
                0
            );
            object empty = Collection(heap, true);
            Check(Long(empty, ReclaimedCollection) == 0 && Long(empty, ReclaimedPayload) == 0);
        }

        return Task.CompletedTask;
    });

    internal static Task IsolationAndRetirement() => RunWithCleanup(async fixture =>
    {
        ProcessRuntime otherRuntime = fixture.CreateRuntime();
        object firstHeap = fixture.CreateHeap(fixture.Runtime), otherHeap = fixture.CreateHeap(otherRuntime);
        IDisposable firstRoots = fixture.EnterRoots(firstHeap), otherRoots = fixture.EnterRoots(otherHeap);
        int firstSlot = Slot(firstRoots), otherSlot = Slot(otherRoots);
        Call(
            firstRoots,
            Set,
            firstSlot,
            new FloatTerm(2.5)
        );
        var first = (FloatTerm)Call(firstRoots, Read, firstSlot)!;
        Call(
            otherRoots,
            Set,
            otherSlot,
            new Integer((BigInteger.One << 130) + 3)
        );
        var other = (Integer)Call(otherRoots, Read, otherSlot)!;
        AssertAllocation(
            firstHeap,
            2,
            0,
            0,
            0
        );
        AssertAllocation(
            otherHeap,
            4,
            0,
            0,
            0
        );
        var portable = (FloatTerm)Call(
            firstHeap,
            Export,
            firstRoots,
            first
        )!;
        Check(portable.Value == first.Value && !ReferenceEquals(portable, first));
        int foreignSlot = Slot(otherRoots);
        Expect<InvalidOperationException>(() => Call(
            otherRoots,
            SetLocal,
            foreignSlot,
            first
        ));
        AssertAllocation(
            otherHeap,
            4,
            0,
            0,
            0
        );
        Collection(firstHeap, false);
        AssertAllocation(
            firstHeap,
            0,
            2,
            0,
            0
        );
        AssertAllocation(
            otherHeap,
            4,
            0,
            0,
            0
        );
        Task retiring = (Task)Call(firstHeap, Retire)!;
        Check(!retiring.IsCompleted && first.Value == 2.5);
        AssertAllocation(
            firstHeap,
            0,
            2,
            0,
            0
        );
        firstRoots.Dispose();
        await retiring.WaitAsync(TimeSpan.FromSeconds(DrainTimeoutSeconds));
        AssertAllocation(
            firstHeap,
            0,
            0,
            0,
            0
        );
        Check(PhysicalWords(firstHeap, Physical) == 0 && portable.Value == 2.5);
        Expect<ObjectDisposedException>(() => _ = first.Value);
        Check(other.Value == (BigInteger.One << 130) + 3);
        AssertAllocation(
            otherHeap,
            4,
            0,
            0,
            0
        );
        Call(otherRoots, Clear, otherSlot);
        object removed = Collection(otherHeap, true);
        Check(Long(removed, ReclaimedCollection) == 4 && Long(removed, ReclaimedPayload) == 0);
        AssertAllocation(
            otherHeap,
            0,
            0,
            0,
            4
        );
    });

    private static int Slot(IDisposable frame) => (int)Call(frame, Reserve)!;

    private static object Collection(object heap, bool major) => Call(
        heap,
        Collect,
        major,
        ushort.MaxValue
    )!;

    private static void AssertAllocation(
        object heap,
        long nursery,
        long survivor,
        long old,
        long reclaimed
    )
    {
        object snapshot = Get(heap, AllocationSnapshot);
        Check(Long(snapshot, Nursery) == nursery && Long(snapshot, Survivor) == survivor
            && Long(snapshot, Old) == old && Long(snapshot, Reclaimed) == reclaimed);
    }

    private static void AssertPayload(
        object heap,
        long nursery,
        long survivor,
        long old,
        long reclaimed
    )
    {
        object snapshot = Get(heap, PayloadSnapshot);
        Check(Long(snapshot, NurseryPayload) == nursery && Long(snapshot, SurvivorPayload) == survivor
            && Long(snapshot, OldPayload) == old && Long(snapshot, ReclaimedPayload) == reclaimed);
    }

    private static long PhysicalWords(object heap, string name) => Long(Get(heap, PhysicalSnapshot), name);

    private static long Long(object value, string name) => (long)Get(value, name);

    private static object Get(object target, string name) => target.GetType().GetProperty(name, Members)!.GetValue(target)!;

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
        private readonly List<ProcessRuntime> runtimes = new();
        private readonly List<object> heaps = new();
        private readonly List<IDisposable> roots = new();
        internal ProcessRuntime Runtime { get; }

        internal Fixture() => Runtime = CreateRuntime();

        internal ProcessRuntime CreateRuntime()
        {
            runtimes.EnsureCapacity(checked(runtimes.Count + 1));
            ProcessRuntime runtime = OwnedGuest506RuntimeFactory.Create();
            runtimes.Add(runtime);

            return runtime;
        }

        internal object CreateHeap(ProcessRuntime runtime)
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
            foreach (ProcessRuntime runtime in runtimes)
            {
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
}
