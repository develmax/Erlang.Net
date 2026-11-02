using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.DistributionNoBacking398Names;

namespace Erlang.Tests;

internal static class DistributionNoBacking398Checks
{
    internal static async Task Run()
    {
        foreach (bool profile in new[] { false, true })
        {
            await using var runtime = OwnedGuest506RuntimeFactory.Create(profile);
            await using var peer = OwnedGuest506RuntimeFactory.Create(profile);
            Term foreign = peer.DistributionPending.BeginPending(Term.A(Node)).Items[1];
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await actor.InvokeAsync(context =>
            {
                object heap = Property(context, Heap);
                TupleTerm handle = runtime.DistributionPending.BeginPending(Term.A(Node));
                Term resource = handle.Items[1];
                Require(StorageOf(heap, resource) is null);
                Require(!(bool)heap.GetType().GetMethod(Magic, Static)!.Invoke(null, [resource])!);
                foreach (var family in DirectFamilies)
                {
                    Type type = typeof(ProcessRuntime).Assembly.GetType(family.Type, true)!;
                    Require(type.GetMethod(family.Method, Static)!.Invoke(null, [resource, runtime]) is null);
                }
                Term stale;
                using (var frame = (IDisposable)Call(heap, Enter)!)
                {
                    Term root = NewTuple(frame, [resource]);
                    AssertFlags(heap, root, true);
                    lock (Property(heap, Gate)) Require((bool)Call(heap, ReadonlyProof, root)!);
                    int slot = (int)Call(frame, Reserve)!;
                    Term chain = Nil.Value;
                    for (int index = 0; index < ChainLength; index++)
                        chain = (Term)Call(
                            frame,
                            DistributionNoBacking398Names.Cons,
                            slot,
                            root,
                            chain
                        )!;
                    AssertFlags(heap, chain, true);
                    object registry = Property(runtime, Registry);
                    object before = Property(registry, Traversal);
                    for (int index = 0; index < Repeats; index++)
                    {
                        IDisposable retained;
                        lock (Property(heap, Gate)) retained = (IDisposable)Call(
                            registry,
                            Retain,
                            heap,
                            chain
                        )!;
                        retained.Dispose();
                    }
                    object after = Property(registry, Traversal);
                    Require(Convert.ToInt64(Property(after, Visits)) - Convert.ToInt64(Property(before, Visits)) == Repeats);
                    Require(Convert.ToInt64(Property(after, Operations)) - Convert.ToInt64(Property(before, Operations)) == Repeats);
                    foreach (Term excluded in new Term[] { foreign, runtime.ReferenceResources.CreateBinary(new byte[] { 1 }) })
                        AssertFlags(heap, NewTuple(frame, [excluded]), false);
                    Call(
                        heap,
                        Collect,
                        true,
                        (ushort)0
                    );
                    Require(ReferenceEquals(((TupleTerm)((Cons)chain).Head).Items[0], resource));
                    using (var actual = runtime.DistributionPending.Acquire(handle)) Require(actual is not null);
                    // Real resource closure changes operation admission, but can
                    // never add child/backing edges to an already sealed leaf.
                    object entry = Property(resource, Entry);
                    Call(entry, Close);
                    bool closedRejected = false;
                    try
                    {
                        using var unexpected = runtime.DistributionPending.Acquire(handle);
                    }
                    catch (ArgumentException)
                    {
                        closedRejected = true;
                    }
                    Require(closedRejected);
                    AssertFlags(heap, NewTuple(frame, [resource]), false);
                    AssertFlags(heap, chain, true);
                    stale = chain;
                }
                Call(
                    heap,
                    Collect,
                    true,
                    (ushort)0
                );
                bool staleRejected = false;
                try
                {
                    lock (Property(heap, Gate)) _ = Call(heap, CheckProof, stale);
                }
                catch (ObjectDisposedException)
                {
                    staleRejected = true;
                }
                Require(staleRejected);

                return Task.FromResult(true);
            });
        }
    }

    private static Term NewTuple(object frame, Term[] values)
        => (Term)Call(
            frame,
            DistributionNoBacking398Names.Tuple,
            (int)Call(frame, Reserve)!,
            values
        )!;

    private static object? StorageOf(object heap, Term value)
        => heap.GetType().GetMethod(Storage, Static)!.Invoke(null, [value]);

    private static void AssertFlags(object heap, Term value, bool expected)
    {
        object storage = StorageOf(heap, value)!;
        Require((bool)Property(storage, BackingProof) == expected && (bool)Property(storage, BinaryProof) == expected);
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
