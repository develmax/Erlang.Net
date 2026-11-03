using System.Reflection;
using Erlang.Compiler;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NoBackingClosure192Names;
using static Erlang.Tests.OwnedMapClosure302Names;

namespace Erlang.Tests;

internal static class OwnedMapClosure302Checks
{
    internal static async Task Run()
    {
        foreach (bool owned in new[] { false, true })
        {
            await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
            await using var foreignRuntime = OwnedGuest506RuntimeFactory.Create(owned);
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await using var foreignActor = new RuntimeNodeFoundationHostActor(foreignRuntime);
            IDisposable? foreignFrame = null;
            MapTerm? foreign = null;
            await foreignActor.InvokeAsync(async context =>
            {
                _ = await Execution.EvaluateAsync(new Expr.Literal(Nil.Value), context);
                object heap = Property(context, Heap);
                foreignFrame = (IDisposable)Call(heap, Enter)!;
                foreign = (MapTerm)Own(foreignFrame, Map(Flat302));

                return true;
            });
            var provider = Provider(runtime);
            object payload = Allocate(runtime, provider.Provider, 0);
            BitString resource = Binary(runtime, payload);
            try
            {
                await actor.InvokeAsync(async context =>
                {
                    _ = await Execution.EvaluateAsync(new Expr.Literal(Nil.Value), context);
                    object heap = Property(context, Heap);
                    MapTerm stale;
                    using (var frame = (IDisposable)Call(heap, Enter)!)
                    {
                        foreach (int size in new[] { Flat302, Hamt302 })
                        {
                            var map = (MapTerm)Own(frame, Map(size));
                            object block = Property(Property(map, MapPayload302), RootPayload302);
                            lock (Property(heap, Gate302)) Require((bool)Call(
                                heap,
                                UseProof302,
                                map,
                                block
                            )!);
                            Flags(
                                block,
                                true,
                                true,
                                true
                            );
                            Require(map.Entries.Count == size && map.Get(Term.I(size - 1)).Equals(Term.I(size - 1)));
                            long small = Accumulate(
                                runtime,
                                heap,
                                frame,
                                map,
                                Small302
                            );
                            long large = Accumulate(
                                runtime,
                                heap,
                                frame,
                                map,
                                Large302
                            );
                            Require(small == Small302 && large == Large302);
                            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
                            {
                                OwnedMapClosure302 = true,
                                PrivateProfile = owned,
                                MapSize = size,
                                SmallCount = Small302,
                                SmallActualVisits = small,
                                LargeCount = Large302,
                                LargeActualVisits = large
                            }));
                        }
                        // A portable/host map with equal data remains outside
                        // the actual payload/header/materialization proof.
                        Term portableParent = (Term)Call(
                            frame,
                            TupleMethod,
                            (int)Call(frame, Reserve)!,
                            new Term[] { Map(Flat302) }
                        )!;
                        Flags(
                            StorageOf(heap, portableParent),
                            false,
                            false,
                            false
                        );
                        BitString inline = (BitString)Own(frame, new BitString(new byte[InlineBytes]));
                        long before = ReferencesOf(runtime);
                        int root = (int)Call(frame, Reserve)!;
                        var inlineMap = (MapTerm)Call(
                            frame,
                            Adopt,
                            root,
                            new MapTerm([new(Term.A(Key302), inline)])
                        )!;
                        Flags(
                            StorageOf(heap, inlineMap),
                            true,
                            false,
                            false
                        );
                        Require(ReferencesOf(runtime) == before + 1);
                        Call(frame, Clear, root);
                        Require(ReferencesOf(runtime) == before);
                        // Real native resource and actual writable input must
                        // retain the old full backing/validation route.
                        var writer = (BitString)typeof(BitString).GetMethod(Writable, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [InlineBytes])!;
                        BitString actualWriter = (BitString)Own(frame, writer);
                        foreach (Term excluded in new Term[] { resource, actualWriter })
                        {
                            var map = (MapTerm)Own(frame, new MapTerm([new(Term.A(Key302), excluded)]));
                            object block = StorageOf(heap, map);
                            Require(!(bool)Property(block, NoBacking) && !(bool)Property(block, NoBinary));
                        }
                        bool rejected = false;
                        try
                        {
                            Call(
                                frame,
                                Set,
                                (int)Call(frame, Reserve)!,
                                foreign!
                            );
                        }
                        catch (InvalidOperationException error) when (error.Message == Foreign)
                        {
                            rejected = true;
                        }
                        Require(rejected);
                        stale = (MapTerm)Own(frame, Map(Flat302));
                    }
                    Call(
                        heap,
                        Collect,
                        true,
                        Major
                    );
                    bool staleRejected = false;
                    try
                    {
                        lock (Property(heap, Gate302)) Call(
                            heap,
                            UseProof302,
                            stale,
                            StorageOf(heap, stale)
                        );
                    }
                    catch (ObjectDisposedException)
                    {
                        staleRejected = true;
                    }
                    Require(staleRejected);

                    return true;
                });
            }
            finally
            {
                Call(payload, SuspensionRootLease821Names.ReleaseNative);
                await runtime.DisposeAsync();
                await ((Task)Property(payload, SuspensionRootLease821Names.Retired)).WaitAsync(NativeResource524Names.Deadline);
                await ((Task)Call(provider.Bindings, SuspensionRootLease821Names.RetireImage)!).WaitAsync(NativeResource524Names.Deadline);
                foreignFrame?.Dispose();
                GC.KeepAlive(resource);
            }
        }
    }

    internal static void AssertLiteralChild(ProcessContext context, Term literal)
    {
        object heap = Property(context, Heap);
        using var frame = (IDisposable)Call(heap, Enter)!;
        var map = (MapTerm)Own(frame, new MapTerm([new(Term.A(Key302), literal)]));
        Flags(
            StorageOf(heap, map),
            false,
            false,
            true
        );
        using var sidecar = (IDisposable?)Call(heap, LiteralSidecar302, map);
        Require(sidecar is not null);
        Term parent = (Term)Call(
            frame,
            ConsMethod,
            (int)Call(frame, Reserve)!,
            map,
            Nil.Value
        )!;
        Flags(
            StorageOf(heap, parent),
            false,
            false,
            true
        );
        using var parentSidecar = (IDisposable?)Call(heap, LiteralSidecar302, parent);
        Require(parentSidecar is not null);
    }

    private static MapTerm Map(int count)
        => new(Enumerable.Range(0, count).Select(value => new KeyValuePair<Term, Term>(Term.I(value), Term.I(value))));

    private static Term Own(object frame, Term value) => (Term)Call(
        frame,
        Adopt,
        (int)Call(frame, Reserve)!,
        value
    )!;

    private static object StorageOf(object heap, Term value)
        => heap.GetType().GetMethod(Storage, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(null, [value])!;

    private static long ReferencesOf(ProcessRuntime runtime) => (long)Property(Property(Property(runtime, Registry), Traversal), References);

    private static long Accumulate(
        ProcessRuntime runtime,
        object heap,
        object frame,
        MapTerm map,
        int count
    )
    {
        int slot = (int)Call(frame, Reserve)!;
        object before = Property(Property(runtime, Registry), Traversal);
        Term value = Nil.Value;
        for (int index = 0; index < count; index++) value = (Term)Call(
            frame,
            ConsMethod,
            slot,
            map,
            value
        )!;
        object after = Property(Property(runtime, Registry), Traversal);
        Flags(
            StorageOf(heap, value),
            true,
            true,
            true
        );
        Require((long)Property(after, Operations) - (long)Property(before, Operations) == count);
        long visits = (long)Property(after, Visits) - (long)Property(before, Visits);
        Call(frame, Clear, slot);

        return visits;
    }

    private static void Flags(
        object block,
        bool noBacking,
        bool noBinary,
        bool literalOnly
    )
    {
        Require((bool)Property(block, NoBacking) == noBacking
            && (bool)Property(block, NoBinary) == noBinary
            && (bool)Property(block, LiteralOnly302) == literalOnly);
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure302);
    }
}
