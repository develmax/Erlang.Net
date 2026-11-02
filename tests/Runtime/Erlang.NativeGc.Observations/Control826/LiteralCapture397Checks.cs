using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.LiteralCapture397Names;

namespace Erlang.Tests;

internal static class LiteralCapture397Checks
{
    private static readonly MethodInfo CaptureMethod = typeof(ProcessRuntime).Assembly.GetType(CaptureType, true)!
        .GetMethod(Capture, BindingFlags.NonPublic | BindingFlags.Static)!;

    internal static async Task Run()
    {
        await DistributionNoBacking398Checks.Run();
        foreach (bool profile in new[] { false, true })
        {
            await RootAuthority487(profile);
            await using var runtime = OwnedGuest506RuntimeFactory.Create(profile);
            await using var peer = OwnedGuest506RuntimeFactory.Create(profile);
            Term foreign = peer.DistributionPending.BeginPending(Term.A(Node)).Items[1];
            await using var peerActor441 = new RuntimeNodeFoundationHostActor(peer);
            ProcessContext peerContext441 = await peerActor441.Ready;
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await actor.InvokeAsync(context =>
            {
                object heap = Property(context, Heap);
                NativePublished441(context, heap, Property(peerContext441, Heap));
                TupleTerm handle = runtime.DistributionPending.BeginPending(Term.A(Node));
                Term resource = handle.Items[1];
                Term stale;
                using (var frame = (IDisposable)Call(heap, Enter)!)
                {
                    Term[] input = [resource];
                    var head = (TupleTerm)NewTuple(frame, input);
                    input[0] = new MapTerm([new(Term.I(1), Term.I(2))]);
                    Require(ReferenceEquals(head.Items[0], resource));
                    Proof(heap, head, true);
                    lock (Property(heap, Gate)) Require((bool)Call(heap, ReadonlyProof, head)!);
                    Term small = Build(frame, head, Small);
                    Term large = Build(frame, head, Large);
                    Proof(heap, small, true);
                    Proof(heap, large, true);
                    for (int index = 0; index < Warmup; index++)
                    {
                        Empty(heap, small);
                        Empty(heap, large);
                    }
                    long first = Measure(heap, small), second = Measure(heap, large);
                    Require(first > 0 && second <= first + first / 2);
                    Console.WriteLine(
                        AllocationDiagnostic,
                        profile,
                        first,
                        second
                    );
                    foreach (Term excluded in new Term[] {
                        foreign, runtime.ReferenceResources.CreateBinary(new byte[] { 1 }),
                        new MapTerm([new(Term.I(1), resource)]),
                        new FunctionTerm(0, (_, _) => ValueTask.FromResult<Term>(Nil.Value)),
                        (BitString)typeof(BitString).GetMethod(Writable, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [32])!
                    })
                    {
                        Proof(heap, NewTuple(frame, [excluded]), false);
                    }
                    object token = Activator.CreateInstance(
                        typeof(ProcessRuntime).Assembly.GetType(TokenType, true)!,
                        [Enum.ToObject(typeof(ProcessRuntime).Assembly.GetType(FlagsType, true)!, 0), Nil.Value, 0L, context.Self, 0L]
                    )!;
                    lock (Property(heap, Gate))
                    {
                        Term mutable = (Term)Call(heap, MutableToken, token)!;
                        Call(
                            frame,
                            Set,
                            (int)Call(frame, Reserve)!,
                            mutable
                        );
                        Proof(heap, NewTuple(frame, [mutable, resource]), false);
                        NativeProof441(heap, mutable, false);
                        NativeProof441(heap, NewTuple(frame, [mutable]), false);
                    }
                    Call(
                        heap,
                        Collect,
                        true,
                        (ushort)0
                    );
                    Require(ReferenceEquals(((TupleTerm)((Cons)large).Head).Items[0], resource));
                    using var lease = runtime.DistributionPending.Acquire(handle);
                    stale = large;
                }
                Call(
                    heap,
                    Collect,
                    true,
                    (ushort)0
                );
                bool rejected = false;
                try
                {
                    using IDisposable? unexpected = CaptureValue(heap, stale);
                }
                catch (ObjectDisposedException)
                {
                    rejected = true;
                }
                Require(rejected);

                return Task.FromResult(true);
            });
        }
    }


    private static async Task RootAuthority487(bool profile)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(profile);
        await using var peer = OwnedGuest506RuntimeFactory.Create(profile);
        await runtime.InitializeModulesAsync(_ => { });
        Term portable = Term.Tuple(Term.A(LiteralAtom487), Term.List(Term.I(LiteralValue487)));
        await using PreparedBeamImage image = runtime.Modules.PrepareBeam(BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(portable)));
        Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
        ModuleCodeImage code = runtime.Modules.CurrentCodeImage(image.Name)!;
        object domain = Property(image, Domain487);
        Term literal = ((IReadOnlyList<Term>)Property(domain, Literals487))[0];
        Task retired = (Task)Property(domain, Retirement487);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var missing = new RuntimeNodeFoundationHostActor(runtime);
        await using var foreign = new RuntimeNodeFoundationHostActor(peer);
        object missingHeap = Property(await missing.Ready, Heap);
        object foreignHeap = Property(await foreign.Ready, Heap);
        IDisposable? frame = null;
        IDisposable? survivor = null;
        object? heap = null;
        try
        {
            await actor.InvokeAsync(async context =>
            {
                using SourceCodeLease admitted = await runtime.Modules.AcquireSourceCode(context, code);
                await context.ExecutionCheckpoint();
                using IDisposable executing = admitted.Attach(image);
                heap = Property(context, Heap);
                frame = (IDisposable)Call(heap, Enter)!;
                Require(ReferenceEquals(Call(
                    frame,
                    Adopt441,
                    Call(frame, Reserve)!,
                    literal
                ), literal));

                return Nil.Value;
            });
            await actor.InvokeAsync(context =>
            {
                object block = StorageOf(heap!, literal);
                object authority;
                lock (Property(heap!, Gate))
                {
                    Require(Property(heap!, Pending487) is null);
                    Require(((System.Collections.IDictionary)Field441(heap!, BlockRoots487)).Count == 0);
                    authority = ((System.Collections.IEnumerable)Property(frame!, Authorities487)).Cast<object>().Single();
                    using var leaf = (IDisposable)Call(
                        authority,
                        MatchingLeaf487,
                        block,
                        null
                    )!;
                    int before = (int)Field441(leaf, References487);
                    using (IDisposable actual = CaptureValue(heap!, literal)!)
                    {
                        Require(ReferenceEquals(actual, leaf));
                        Require((int)Field441(leaf, References487) == before + 1);
                    }
                    Require((int)Field441(leaf, References487) == before);
                    using ((IDisposable)Call(heap!, Admission487, leaf)!)
                    {
                        using (IDisposable actual = CaptureValue(heap!, literal)!)
                            Require(ReferenceEquals(actual, leaf) && (int)Field441(leaf, References487) == before + 1);
                        Require((int)Field441(leaf, References487) == before);
                    }
                    using (IDisposable carried = CaptureValue(heap!, literal, authority)!)
                        Require((bool)Call(carried, Owns487, block)!);
                }
                // This creates and disposes a REAL independently owned capture;
                // no private reference counter, lease or proof flag is forged.
                using (IDisposable outside = CaptureValue(heap!, literal)!)
                    Require((bool)Call(outside, Owns487, block)!);
                IDisposable disposedPending = CaptureValue(heap!, literal)!;
                disposedPending.Dispose();
                Require((int)Field441(disposedPending, References487) == 0);
                lock (Property(heap!, Gate))
                    using ((IDisposable)Call(heap!, Admission487, disposedPending)!)
                        Refused487(heap!, literal, typeof(ObjectDisposedException));
                lock (Property(missingHeap, Gate)) Refused487(missingHeap, literal, typeof(InvalidOperationException));
                lock (Property(foreignHeap, Gate)) Refused487(foreignHeap, literal, typeof(InvalidOperationException));
                Term processTuple = NewTuple(frame!, [literal]);
                Require(ReferenceEquals(((TupleTerm)processTuple).Items[0], literal));
                frame!.Dispose();
                frame = null;
                lock (Property(heap!, Gate))
                {
                    Require(Property(heap!, Pending487) is null);
                    Require(!((System.Collections.IEnumerable)Field441(heap!, Frames487)).Cast<object>()
                        .SelectMany(current => ((System.Collections.IEnumerable)Property(current, Authorities487)).Cast<object>()).Any());
                    Require(((System.Collections.IDictionary)Field441(heap!, BlockRoots487)).Count > 0);
                    survivor = CaptureValue(heap!, literal);
                    Require(survivor is not null && (bool)Call(survivor, Owns487, block)!);
                }
                Call(
                    heap!,
                    Collect,
                    true,
                    (ushort)0
                );
                lock (Property(heap!, Gate)) Require(((System.Collections.IDictionary)Field441(heap!, BlockRoots487)).Count == 0);
                Require(literal.Equals(portable));

                return Task.FromResult(true);
            });
            // The empty receiver's rejection checks are complete. Its host
            // channel loop deliberately does not pump pending mailbox signals;
            // finish that helper process before the purger scans live actors.
            await missing.DisposeAsync();
            _ = runtime.Modules.DeleteModule(image.Name);
            await actor.InvokeAsync(async context =>
            {
                Require(await (ValueTask<bool>)Call(
                    runtime,
                    Purge487,
                    context,
                    image.Name
                )!);
                await (Task)typeof(ModuleLiteral801Checks).GetMethod(AwaitReply487, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [context])!;
                Require(!retired.IsCompleted && literal.Equals(portable));
                survivor!.Dispose();
                survivor = null;

                return Nil.Value;
            });
            await retired.WaitAsync(TimeSpan.FromSeconds(RetirementSeconds487));
            await actor.InvokeAsync(context =>
            {
                // A stale arena facade still exposes its storage identity.
                // Capture under H rejects missing actual authority as ForeignRoot;
                // dereferencing the retired native cell is separately disposed.
                lock (Property(heap!, Gate)) Refused487(heap!, literal, typeof(InvalidOperationException));
                bool disposed = false;
                try
                {
                    _ = ((TupleTerm)literal).Items[0];
                }
                catch (ObjectDisposedException)
                {
                    disposed = true;
                }
                Require(disposed);

                return Task.FromResult(true);
            });
        }
        finally
        {
            survivor?.Dispose();
            frame?.Dispose();
        }
    }

    private static void Refused487(object heap, Term literal, Type expected)
    {
        Exception? caught = null;
        try
        {
            using IDisposable? unexpected = CaptureValue(heap, literal);
        }
        catch (Exception error)
        {
            caught = error;
        }
        Require(caught is not null && expected.IsInstanceOfType(caught));
    }

    private static void ActiveChosenLease466(
        ProcessContext context,
        object heap,
        BitString binary,
        byte[] expected
    )
    {
        object registry = Property(context.Runtime, NoBackingClosure192Names.Registry);
        long baselineReferences = (long)Property(Property(registry, NoBackingClosure192Names.Traversal), NoBackingClosure192Names.References);
        int baselineFrames = (int)Property(Property(heap, Snapshot466), RootFrames466);
        using var chosenFrame = (IDisposable)Call(heap, Enter)!;
        using var hitFrame = (IDisposable)Call(heap, Enter)!;
        using var queuedFrame = (IDisposable)Call(heap, Enter)!;
        using var middleFrame = (IDisposable)Call(heap, Enter)!;
        using var tailFrame = (IDisposable)Call(heap, Enter)!;
        using var fallbackFrame = (IDisposable)Call(heap, Enter)!;
        using var buildFrame = (IDisposable)Call(heap, Enter)!;
        Term graph = Build(buildFrame, binary, SummaryCount466);

        long Visits() => (long)Property(Property(registry, NoBackingClosure192Names.Traversal), NoBackingClosure192Names.Visits);
        long before;
        // Drop the construction's actual summary while H excludes physical GC,
        // then capture the same still-live native graph with no eligible owner.
        lock (Property(heap, Gate))
        {
            buildFrame.Dispose();
            before = Visits();
            Call(
                chosenFrame,
                Set,
                (int)Call(chosenFrame, Reserve)!,
                graph
            );
            Require(Visits() - before == SummaryCount466 + 2);
        }
        object registryGate = Field441(registry, RegistryGate466);
        var summaries = (System.Collections.IDictionary)Field441(registry, SummaryTable466);
        object chosen;
        lock (registryGate) chosen = summaries[graph] ?? throw new InvalidOperationException();
        var arrays = new List<Array>();

        void Fresh(object lease)
        {
            Array handles = (Array)Property(lease, Handles466);
            Require(handles.Length == 1 && arrays.All(previous => !ReferenceEquals(previous, handles)));
            arrays.Add(handles);
        }

        void Ring(params object[] ordered)
        {
            lock (registryGate)
            {
                Require(ordered.Length > 0 && ReferenceEquals(summaries[graph], ordered[0]));
                for (int index = 0; index < ordered.Length; index++)
                {
                    object lease = ordered[index];
                    Require(ReferenceEquals(Property(lease, SummaryRoot495), graph));
                    Require(ReferenceEquals(Property(lease, Previous495), ordered[(index + ordered.Length - 1) % ordered.Length]));
                    Require(ReferenceEquals(Property(lease, Next495), ordered[(index + 1) % ordered.Length]));
                    Require(index == 0 ? ReferenceEquals(Property(lease, CachedRoot466), graph) : Property(lease, CachedRoot466) is null);
                }
            }
        }

        void Released(object lease)
        {
            lock (registryGate)
            {
                Require(Property(lease, CachedRoot466) is null && Property(lease, SummaryRoot495) is null
                    && Property(lease, Previous495) is null && Property(lease, Next495) is null);
                Require(((Array)Property(lease, Handles466)).Length == 0);
            }
        }

        object Hit(object frame)
        {
            long first = Visits();
            Call(
                frame,
                Set,
                (int)Call(frame, Reserve)!,
                graph
            );
            Require(Visits() - first == 1);
            object binaryFrame = Field441(frame, BinaryFrame466);
            object lease = ((System.Collections.IDictionary)Field441(binaryFrame, BinaryRoots466)).Values.Cast<object>().Single();
            Fresh(lease);

            return lease;
        }
        Fresh(chosen);
        Ring(chosen);
        object hit = Hit(hitFrame);
        Ring(chosen, hit);
        chosenFrame.Dispose();
        Released(chosen);
        Ring(hit);
        object queued = Hit(queuedFrame);
        Ring(hit, queued);
        object middle = Hit(middleFrame);
        object tail = Hit(tailFrame);
        Ring(
            hit,
            queued,
            middle,
            tail
        );
        middleFrame.Dispose();
        Released(middle);
        Ring(hit, queued, tail);
        tailFrame.Dispose();
        Released(tail);
        Ring(hit, queued);
        hitFrame.Dispose();
        Released(hit);
        Ring(queued);
        Call(
            heap,
            Collect,
            true,
            (ushort)0
        );
        Require(((BitString)((Cons)graph).Head).ToArray().AsSpan().SequenceEqual(expected));
        lock (Property(heap, Gate))
        {
            queuedFrame.Dispose();
            Released(queued);
            lock (registryGate) Require(!summaries.Contains(graph));
            // All eligible members are gone. H excludes physical GC until
            // full graph capture reinstates actual frame custody.
            before = Visits();
            Call(
                fallbackFrame,
                Set,
                (int)Call(fallbackFrame, Reserve)!,
                graph
            );
            Require(Visits() - before == SummaryCount466 + 2);
        }
        object replacement;
        lock (registryGate) replacement = summaries[graph]!;
        Fresh(replacement);
        Ring(replacement);
        Require(!ReferenceEquals(replacement, chosen) && !ReferenceEquals(replacement, hit)
            && !ReferenceEquals(replacement, queued) && !ReferenceEquals(replacement, middle) && !ReferenceEquals(replacement, tail));
        Call(
            heap,
            Collect,
            true,
            (ushort)0
        );
        Require(((BitString)((Cons)graph).Head).ToArray().AsSpan().SequenceEqual(expected));
        fallbackFrame.Dispose();
        Released(replacement);
        lock (registryGate) Require(!summaries.Contains(graph));
        Require(arrays.All(handles => handles.Cast<object?>().All(handle => handle is null)));
        Call(
            heap,
            Collect,
            true,
            (ushort)0
        );
        bool stale = false;
        try
        {
            _ = ((Cons)graph).Head;
        }
        catch (ObjectDisposedException)
        {
            stale = true;
        }
        Require(stale && binary.ToArray().AsSpan().SequenceEqual(expected));
        Require((long)Property(Property(registry, NoBackingClosure192Names.Traversal), NoBackingClosure192Names.References) == baselineReferences);
        Require((int)Property(Property(heap, Snapshot466), RootFrames466) == baselineFrames);
    }

    private static void NativePublished441(ProcessContext context, object heap, object foreignHeap)
    {
        Term stale;
        byte[] bytes = Enumerable.Range(0, OrdinaryBytes441).Select(index => (byte)index).ToArray();
        using (var frame = (IDisposable)Call(heap, Enter)!)
        {
            // Import the COMPLETE input graph, including the actual Nil tail.
            // Its genuine construction transaction creates and roots this Cons
            // itself; do not manufacture a new child outside the input custody.
            using var import = (IDisposable)Call(frame, PrepareImport441,
                new Cons(new BitString(bytes), Nil.Value))!;
            Term uncommitted = (Term)Property(import, Value441);
            var binary = (BitString)((Cons)uncommitted).Head;
            lock (Property(heap, Gate))
            {
                object descriptor = Call(heap, Descriptor441, Property(binary, BitsStorage441))!;
                object binRef = Property(descriptor, BinRef441);
                Require(!(bool)Property(descriptor, Published441) && !(bool)Property(binRef, Published441));
                NativeProof441(heap, uncommitted, false);
                using ((IDisposable)Call(
                    heap,
                    PrepareNative441,
                    uncommitted,
                    true
                )!) { }
                Require(!(bool)Property(descriptor, Published441) && !(bool)Property(binRef, Published441));
                using (var publication = (IDisposable)Call(
                    heap,
                    PrepareNative441,
                    uncommitted,
                    true
                )!)
                    Call(publication, CommitNative441);
                Require((bool)Property(descriptor, Published441) && (bool)Property(binRef, Published441));
                // Constructor certificates never retroactively change.
                NativeProof441(heap, uncommitted, false);
            }
            Call(
                frame,
                Set,
                (int)Call(frame, Reserve)!,
                uncommitted
            );
            ActiveChosenLease466(
                context,
                heap,
                binary,
                bytes
            );
            Term small = Build(frame, binary, Small), large = Build(frame, binary, Large);
            NativeProof441(heap, small, true);
            NativeProof441(heap, large, true);
            NativeProof441(foreignHeap, large, false);
            lock (Property(heap, Gate))
            {
                for (int index = 0; index < Warmup; index++)
                {
                    using var first = (IDisposable?)Call(
                        heap,
                        PrepareNative441,
                        small,
                        true
                    );
                    using var second = (IDisposable?)Call(
                        heap,
                        PrepareNative441,
                        large,
                        true
                    );
                    Require(first is null && second is null);
                }
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int index = 0; index < Repeats; index++)
                    using ((IDisposable?)Call(
                        heap,
                        PrepareNative441,
                        small,
                        true
                    )) { }
                long smallBytes = GC.GetAllocatedBytesForCurrentThread() - before;
                before = GC.GetAllocatedBytesForCurrentThread();
                for (int index = 0; index < Repeats; index++)
                    using ((IDisposable?)Call(
                        heap,
                        PrepareNative441,
                        large,
                        true
                    )) { }
                long largeBytes = GC.GetAllocatedBytesForCurrentThread() - before;
                Require(largeBytes <= smallBytes + smallBytes / 2);
            }
            int callbacks = 0;
            var function = new FunctionTerm(0, (_, _) =>
            {
                callbacks++;

                return ValueTask.FromResult<Term>(Nil.Value);
            });
            var writable = (BitString)typeof(BitString).GetMethod(Writable, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [OrdinaryBytes441])!;
            foreach (Term excluded in new Term[] {
                writable, context.Runtime.ReferenceResources.CreateBinary(bytes),
                function, new MapTerm([new(Term.I(1), binary)])
            })
                NativeProof441(heap, NewTuple(frame, [excluded]), false);
            Require(callbacks == 0);
            var map = (MapTerm)Call(
                frame,
                Adopt441,
                (int)Call(frame, Reserve)!,
                new MapTerm([new(Term.I(1), binary), new(Term.I(2), binary)])
            )!;
            object mapBlock = Property(Property(map, MapPayload441), MapRoot441);
            // A manufactured tuple facade over a REAL live map allocation is
            // not a Cons/Tuple constructor certificate or map read capability.
            var overlay = (TupleTerm)Activator.CreateInstance(
                typeof(TupleTerm),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                [mapBlock],
                null
            )!;
            lock (Property(heap, Gate))
                Require(!(bool)Call(
                    heap,
                    ExistingClosureGate441,
                    overlay,
                    mapBlock
                )!);
            NativeProof441(heap, overlay, false);
            Call(
                heap,
                Collect,
                true,
                (ushort)0
            );
            Require(((BitString)((Cons)large).Head).ToArray().SequenceEqual(bytes));
            stale = large;
        }
        Call(
            heap,
            Collect,
            true,
            (ushort)0
        );
        bool rejected = false;
        try
        {
            lock (Property(heap, Gate))
                _ = Call(heap, UseNativeProof441, stale);
        }
        catch (ObjectDisposedException)
        {
            rejected = true;
        }
        Require(rejected);
    }

    private static void NativeProof441(object heap, Term value, bool expected)
    {
        lock (Property(heap, Gate))
            Require((bool)Call(heap, UseNativeProof441, value)! == expected);
    }

    private static object Field441(object value, string name)
        => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.GetValue(value)!;

    internal static void AssertLiteralChild(ProcessContext context, Term literal)
    {
        object heap = Property(context, Heap);
        TupleTerm handle = context.Runtime.DistributionPending.BeginPending(Term.A(Node));
        using var frame = (IDisposable)Call(heap, Enter)!;
        var literalOnly441 = (TupleTerm)NewTuple(frame, [literal]);
        NativeProof441(heap, literalOnly441, true);
        var mixed = (TupleTerm)NewTuple(frame, [literal, handle.Items[1]]);
        Proof(heap, mixed, true);
        object storage = StorageOf(heap, mixed);
        Require(!(bool)Property(storage, EmptyProof));
        using IDisposable? actual = CaptureValue(heap, mixed);
        Require(actual is not null);
        Call(
            heap,
            Collect,
            true,
            (ushort)0
        );
        Require(ReferenceEquals(mixed.Items[0], literal));
        using var lease = context.Runtime.DistributionPending.Acquire(handle);
    }

    private static Term NewTuple(object frame, Term[] children)
        => (Term)Call(
            frame,
            TupleMethod,
            (int)Call(frame, Reserve)!,
            children
        )!;

    private static Term Build(object frame, Term head, int count)
    {
        int slot = (int)Call(frame, Reserve)!;
        Term value = Nil.Value;
        for (int index = 0; index < count; index++) value = (Term)Call(
            frame,
            ConsMethod,
            slot,
            head,
            value
        )!;

        return value;
    }

    private static object StorageOf(object heap, Term value)
        => heap.GetType().GetMethod(Storage, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(null, [value])!;

    private static void Proof(object heap, Term value, bool expected)
        => Require((bool)Property(StorageOf(heap, value), ProofProperty) == expected);

    private static long Measure(object heap, Term value)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < Repeats; index++) Empty(heap, value);

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static void Empty(object heap, Term value)
    {
        using IDisposable? actual = CaptureValue(heap, value);
        Require(actual is null);
    }

    private static IDisposable? CaptureValue(object heap, Term value, object? carried = null)
    {
        try
        {
            return (IDisposable?)CaptureMethod.Invoke(null, [heap, value, carried]);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }
}
