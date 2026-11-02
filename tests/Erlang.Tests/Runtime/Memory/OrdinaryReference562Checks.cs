using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.OrdinaryReference562TestNames;

namespace Erlang.Tests;

internal static class OrdinaryReference562Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static Task PhysicalAndRetirement() => RunWithCleanup(async fixture =>
    {
        object heap = fixture.CreateHeap();
        IDisposable roots = fixture.EnterRoots(heap);
        ReferenceTerm source = fixture.Runtime.MakeReference();
        uint[] original = source.Words.ToArray();
        BigInteger originalId = source.FullId;
        int slot = (int)Call(roots, Reserve)!;
        Call(
            roots,
            Set,
            slot,
            source
        );
        var owned = (ReferenceTerm)Call(roots, Read, slot)!;
        Check(!ReferenceEquals(owned, source) && owned.FullId == originalId && owned.Words.SequenceEqual(original));
        Call(heap, Collect);
        Check(Words(heap, Physical) == 3 && Words(heap, Headers) == 1
            && Words(heap, Numeric) == 2 && Words(heap, Handles) == 0);
        object storage = Get(owned, Storage);
        object cells = Get(Get(storage, Slab), Cells);
        int offset = (int)Get(storage, Offset);
        nuint first;
        lock (Get(heap, HeapGate))
        {
            first = (nuint)Call(cells, ReadNumeric, offset + 1)!;
            Call(
                cells,
                WriteNumeric,
                new nuint[] { first ^ NumberMutation },
                offset + 1
            );
        }
        Check(owned.Words[0] == (original[0] ^ 1u) && owned.FullId != originalId);
        lock (Get(heap, HeapGate))
            Call(
                cells,
                WriteNumeric,
                new nuint[] { first ^ 1u },
                offset + 1
            );
        Expect<InvalidOperationException>(() => _ = owned.Words);
        nuint header;
        lock (Get(heap, HeapGate))
        {
            Call(
                cells,
                WriteNumeric,
                new nuint[] { first },
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
        Expect<InvalidOperationException>(() => _ = owned.FullId);
        lock (Get(heap, HeapGate))
            Call(
                cells,
                WriteHeaders,
                new nuint[] { header },
                offset
            );
        var portable = (ReferenceTerm)Call(
            heap,
            Export,
            roots,
            owned
        )!;
        Check(portable.FullId == originalId && Property(portable, Storage) is null);
        Call(
            heap,
            CollectGeneration,
            false,
            ushort.MaxValue
        );
        Call(
            heap,
            CollectGeneration,
            false,
            ushort.MaxValue
        );
        Call(roots, Clear, slot);
        Call(
            heap,
            CollectGeneration,
            false,
            ushort.MaxValue
        );
        Check(Words(heap, Physical) == 3 && owned.FullId == originalId);
        Call(
            roots,
            Set,
            slot,
            owned
        );
        Task retirement = (Task)Call(heap, Retire)!;
        Check(!retirement.IsCompleted && owned.FullId == originalId);
        roots.Dispose();
        await retirement.WaitAsync(TimeSpan.FromSeconds(DrainTimeoutSeconds));
        Expect<ObjectDisposedException>(() => _ = owned.Words);
        Check(portable.FullId == originalId && source.Words.SequenceEqual(original));
    });

    internal static Task AdoptionImportAndRollback() => RunWithCleanup(async fixture =>
    {
        object heap = fixture.CreateHeap();
        IDisposable roots = fixture.EnterRoots(heap);
        int slot = (int)Call(roots, Reserve)!;
        ReferenceTerm source = fixture.Runtime.MakeReference();
        var pair = (TupleTerm)Call(
            roots,
            Adopt,
            slot,
            Term.Tuple(source, source)
        )!;
        Check(ReferenceEquals(pair.Items[0], pair.Items[1]) && !ReferenceEquals(pair.Items[0], source));
        Check(Words(heap, Physical) == 6 && Words(heap, Numeric) == 2);
        Call(roots, Clear, slot);
        Call(heap, Collect);
        Check(Words(heap, Physical) == 0);
        foreach (Term value in new Term[]
        {
            Term.Tuple(source, source, Term.I(1)),
            Term.List(source, Term.I(1)),
            new MapTerm(new KeyValuePair<Term, Term>[] { new(source, source) })
        })
        {
            foreach (string route in new[] { Set, SetPrepared, Adopt })
            {
                Call(
                    roots,
                    route,
                    slot,
                    value
                );
                Term actual = (Term)Call(roots, Read, slot)!;
                Call(heap, Collect);
                var exported = (Term)Call(
                    heap,
                    Export,
                    roots,
                    actual
                )!;
                Check(exported.Equals(value) && Words(heap, Numeric) >= 2);
                Call(roots, Clear, slot);
                Call(heap, Collect);
                Check(Words(heap, Physical) == 0);
            }
        }
        IDisposable failed = fixture.EnterRoots(heap);
        object custody = Call(failed, BeginConstruction, source)!;
        failed.Dispose();
        lock (Get(heap, HeapGate))
            Expect<ObjectDisposedException>(() => Call(
                heap,
                CreateReference,
                source,
                custody
            ));
        Check(Words(heap, Physical) == 0);
        roots.Dispose();
        await ((Task)Call(heap, Retire)!).WaitAsync(TimeSpan.FromSeconds(DrainTimeoutSeconds));
    });

    internal static Task ProvenanceIdentityAndForeign() => RunWithCleanup(async fixture =>
    {
        ProcessRuntime runtime = fixture.Runtime;
        object heap = fixture.CreateHeap();
        object otherHeap = fixture.CreateHeap();
        IDisposable roots = fixture.EnterRoots(heap);
        IDisposable otherRoots = fixture.EnterRoots(otherHeap);
        int slot = (int)Call(roots, Reserve)!;
        int otherSlot = (int)Call(otherRoots, Reserve)!;
        ReferenceTerm source = runtime.MakeReference();
        ReferenceTerm publicSame = new(source.Node, source.Words, source.Creation);
        ReferenceTerm derived = new PublicReference562(source);
        ResourceReferenceTerm resource = runtime.ReferenceResources.CreateBinary(new byte[] { 1 });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ReferenceTerm alias = await actor.Invoke(context => runtime.CreateAlias(context, false, false));
        await using var foreignRuntime = OwnedGuest506RuntimeFactory.Create();
        ReferenceTerm foreign = foreignRuntime.MakeReference();
        Check(foreign.Node == source.Node && foreign.Creation == source.Creation);
        foreach (ReferenceTerm refused in new ReferenceTerm[] { publicSame, derived, resource, alias, foreign })
        {
            Check((bool)Call(heap, Eligible, refused)! == false);
            bool external = (bool)Call(heap, ExternalEligible, refused)!;
            bool pidAlias = (bool)Call(heap, AliasEligible, refused)!;
            var actual = (ReferenceTerm)Call(
                roots,
                Adopt,
                slot,
                refused
            )!;
            if (external || pidAlias)
            {
                // Different physical families must not manufacture ordinary
                // local562 provenance merely because the numeric values match.
                Check(!ReferenceEquals(actual, refused) && actual.Equals(refused));
                Check(!(bool)Property(actual, OrdinaryLocal)!);
                Check((Property(actual, ExternalStorage) is not null) == external);
                Check((Property(actual, Storage) is not null) == pidAlias);
                Check((bool)Call(heap, Eligible, actual)! == false && Words(heap, Physical) > 0);
            }
            else
                Check(ReferenceEquals(actual, refused) && Property(refused, Storage) is null && Words(heap, Physical) == 0);
            Call(roots, Clear, slot);
            Call(heap, Collect);
            Check(Words(heap, Physical) == 0);
        }
        Call(
            roots,
            Adopt,
            slot,
            source
        );
        var owned = (ReferenceTerm)Call(roots, Read, slot)!;
        Expect<InvalidOperationException>(() => Call(
            otherRoots,
            Adopt,
            otherSlot,
            owned
        ));
        Check(Words(otherHeap, Physical) == 0);
        IDisposable foreignConstruction = fixture.EnterRoots(otherHeap);
        object foreignCustody = Call(foreignConstruction, BeginConstruction, source)!;
        lock (Get(otherHeap, HeapGate))
            Expect<InvalidOperationException>(() => Call(
                otherHeap,
                CreateReference,
                owned,
                foreignCustody
            ));
        Check(Words(otherHeap, Physical) == 0);
        foreignConstruction.Dispose();
        object identity = Get(runtime, LocalIdentity);
        object before = Get(identity, IdentitySnapshot);
        string node = (string)Get(before, Node);
        uint creation = (uint)Get(before, Creation);
        try
        {
            // Actual stable identity publication seam, not full net_kernel boot.
            Call(
                identity,
                PublishIdentity,
                ChangedNode,
                creation + 1
            );
            Check(source.Node == ChangedNode && owned.Node == ChangedNode);
            Check((bool)Call(heap, Eligible, source)! && (bool)Call(heap, Eligible, owned)!);
            var portable = (ReferenceTerm)Call(
                heap,
                Export,
                roots,
                owned
            )!;
            Call(
                otherRoots,
                Set,
                otherSlot,
                portable
            );
            var copied = (ReferenceTerm)Call(otherRoots, Read, otherSlot)!;
            Check(copied.FullId == source.FullId && copied.Creation == creation + 1
                && copied.Node == ChangedNode && Property(copied, Storage) is not null);
            Call(otherRoots, Clear, otherSlot);
        }
        finally
        {
            Call(
                identity,
                PublishIdentity,
                node,
                creation
            );
        }
        Check(await actor.Invoke(context => runtime.Unalias(context, alias)));
        roots.Dispose();
        otherRoots.Dispose();
        await ((Task)Call(heap, Retire)!).WaitAsync(TimeSpan.FromSeconds(DrainTimeoutSeconds));
        await ((Task)Call(otherHeap, Retire)!).WaitAsync(TimeSpan.FromSeconds(DrainTimeoutSeconds));
    });

    private sealed class PublicReference562(ReferenceTerm source)
        : ReferenceTerm(source.Node, source.Words, source.Creation)
    {
    }

    private static object? Property(ReferenceTerm target, string property)
        => typeof(ReferenceTerm).GetProperty(property, Members)!.GetValue(target);

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
