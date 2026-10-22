using System.Runtime.CompilerServices;
using Erlang;

namespace Erlang.Tests;

internal static class EtsPayloadDrop406Checks
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (long Slot, WeakReference<TupleTerm> Composite, WeakReference<ReferenceTerm> Resource) Store(object storage, ProcessContext context, Term table)
    {
        ResourceReferenceTerm resource = context.Runtime.ReferenceResources.CreateBinary(new byte[] { 4, 0, 6 });
        var child = Term.Tuple(resource, Term.I(406));
        var root = Term.Tuple(child, child, resource);
        long slot = EtsFoundation405Access.Store(
            storage,
            context,
            table,
            root
        );

        return (slot, new(root), new(resource));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Alive<T>(WeakReference<T> reference) where T : class => reference.TryGetTarget(out _);

    private static void Collected<T>(WeakReference<T> reference) where T : class
    {
        for (int index = 0; index < EtsPayloadDrop406Names.CollectionAttempts && Alive(reference); index++) EtsFoundation405Access.Collect();
        EtsFoundation405Access.Require(!Alive(reference));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Read(object lease, ProcessRuntime runtime)
    {
        var root = (TupleTerm)EtsFoundation405Access.Invoke(lease, EtsPayloadDrop406Names.Materialize, CancellationToken.None)!;
        EtsFoundation405Access.Require(ReferenceEquals(root.Items[0], root.Items[1]));
        using var resource = runtime.ReferenceResources.AcquireBinary((ReferenceTerm)root.Items[2]);

        return resource is not null && resource.Value.ToArray().SequenceEqual(new byte[] { 4, 0, 6 });
    }

    internal static async Task LastReadLeaseRoots()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        object storage = EtsFoundation405Access.Get(runtime);
        Term table = await actor.Invoke(context => EtsFoundation405Access.Create(storage, context));
        var stored = await actor.Invoke(context => Store(storage, context, table));
        Collected(stored.Composite); // the stored descriptor graph did not retain the source tuple
        object lease = await actor.Invoke(context => EtsFoundation405Access.Invoke(
            storage,
            EtsPayloadDrop406Names.Acquire,
            context,
            table,
            stored.Slot
        )!);
        await actor.Invoke(context =>
 {
     EtsFoundation405Access.Invoke(
         storage,
         EtsPayloadDrop406Names.Destroy,
         context,
         table
     );

     return true;
 });
        var snapshot = EtsFoundation405Access.Snapshot(storage);
        EtsFoundation405Access.Require((int)snapshot[0]! == 0 && (long)snapshot[3]! == 1 && (long)snapshot[4]! > 0);
        EtsFoundation405Access.Collect();
        EtsFoundation405Access.Require(Alive(stored.Resource) && Read(lease, runtime));
        ((IDisposable)lease).Dispose();
        ((IDisposable)lease).Dispose();
        EtsFoundation405Access.Empty(storage);
        Collected(stored.Resource);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Term?[] CopyAndRelease(object lease)
    {
        Term copy = (Term)EtsFoundation405Access.Invoke(lease, EtsPayloadDrop406Names.Materialize, CancellationToken.None)!;
        ((IDisposable)lease).Dispose();

        return new Term?[] { copy };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CopyReadable(Term?[] copy, ProcessRuntime runtime)
    {
        var tuple = (TupleTerm)copy[0]!;
        using var resource = runtime.ReferenceResources.AcquireBinary((ReferenceTerm)tuple.Items[2]);

        return resource is not null && resource.Value.ToArray().SequenceEqual(new byte[] { 4, 0, 6 });
    }

    internal static async Task CopyOutRoots()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        object storage = EtsFoundation405Access.Get(runtime);
        Term table = await actor.Invoke(context => EtsFoundation405Access.Create(storage, context));
        var stored = await actor.Invoke(context => Store(storage, context, table));
        Collected(stored.Composite);
        object lease = await actor.Invoke(context => EtsFoundation405Access.Invoke(
            storage,
            EtsPayloadDrop406Names.Acquire,
            context,
            table,
            stored.Slot
        )!);
        await actor.Invoke(context =>
 {
     EtsFoundation405Access.Invoke(
         storage,
         EtsPayloadDrop406Names.Destroy,
         context,
         table
     );

     return true;
 });
        Term?[] copy = CopyAndRelease(lease);
        EtsFoundation405Access.Empty(storage);
        EtsFoundation405Access.Collect();
        EtsFoundation405Access.Require(Alive(stored.Resource) && CopyReadable(copy, runtime));
        copy[0] = null;
        Collected(stored.Resource);
    }
}
