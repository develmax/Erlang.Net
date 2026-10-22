using System.Numerics;
using System.Runtime.CompilerServices;
using Erlang;

namespace Erlang.Tests;

internal static class EtsFoundation405Checks
{
    internal static async Task DetachedGraphAndRollback()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var foreign = new ProcessRuntime(1);
        EtsFoundation405Access.Require(EtsFoundation405Access.Unallocated(runtime));
        int exports = runtime.Modules.Exports.Count;
        object store = EtsFoundation405Access.Get(runtime);
        EtsFoundation405Access.Empty(store);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var foreignActor = new RuntimeNodeFoundationHostActor(foreign);
        Term table = await actor.Invoke(context => EtsFoundation405Access.Create(store, context));
        var foreignContext = await foreignActor.Ready;
        object foreignStorage = EtsFoundation405Access.Get(foreign);
        Term foreignTable = await foreignActor.Invoke(context => EtsFoundation405Access.Create(foreignStorage, context));
        var partial = await actor.Invoke(context =>
        {
            Term deep = Nil.Value;
            for (int index = 0; index < EtsFoundation405Names.DeepConsCount; index++) deep = new Cons(Term.I(index), deep);
            var shared = Term.Tuple(Term.A(EtsFoundation405Names.Marker), new Integer(BigInteger.One << EtsFoundation405Names.BigIntegerShift));
            var source = Term.Tuple(
                deep,
                shared,
                shared,
                new BitString(new byte[] { 0xAB, 0x80 }, 9),
                new FloatTerm(-0.0)
            );
            long id = EtsFoundation405Access.Store(
                store,
                context,
                table,
                source
            );
            var copied = (TupleTerm)EtsFoundation405Access.Read(
                store,
                context,
                table,
                id
            );
            EtsFoundation405Access.Require(!ReferenceEquals(source, copied) && !ReferenceEquals(source.Items[1], copied.Items[1]));
            EtsFoundation405Access.Require(ReferenceEquals(copied.Items[1], copied.Items[2]));
            EtsFoundation405Access.Require(Cons.Items(copied.Items[0]).Count() == EtsFoundation405Names.DeepConsCount);
            EtsFoundation405Access.Require(copied.Items[1].Equals(shared) && copied.Items[2].Equals(shared));
            EtsFoundation405Access.Require(copied.Items[3].Equals(source.Items[3])
                && BitConverter.DoubleToInt64Bits(((FloatTerm)copied.Items[4]).Value) == BitConverter.DoubleToInt64Bits(-0.0));
            var before = EtsFoundation405Access.Snapshot(store);
            var fun = new FunctionTerm(0, static (_, _) => throw new InvalidOperationException(EtsFoundation405Names.Failure));
            WeakReference<ReferenceTerm> rejectedResource = RejectAfterResourcePrefix(
                store,
                context,
                table,
                fun
            );
            bool rejectedFunction = false;
            try
            {
                EtsFoundation405Access.Store(
                    store,
                    context,
                    table,
                    Term.Tuple(Term.I(1), fun)
                );
            }
            catch (NotSupportedException error) when (error.Message == EtsFoundation405Names.UnsupportedFunction)
            {
                rejectedFunction = true;
            }
            EtsFoundation405Access.Require(rejectedFunction && before.Equals(EtsFoundation405Access.Snapshot(store)));
            bool rejectedDomain = false;
            try
            {
                EtsFoundation405Access.Store(
                    store,
                    context,
                    table,
                    Term.Tuple(foreignContext.Self)
                );
            }
            catch (ArgumentException)
            {
                rejectedDomain = true;
            }
            EtsFoundation405Access.Require(rejectedDomain && before.Equals(EtsFoundation405Access.Snapshot(store)));
            bool rejectedActor = false;
            try
            {
                EtsFoundation405Access.Create(store, foreignContext);
            }
            catch (ArgumentException error) when (error.Message == EtsFoundation405Names.Foreign)
            {
                rejectedActor = true;
            }
            EtsFoundation405Access.Require(rejectedActor);
            bool foreignTableRejected = false;
            try
            {
                EtsFoundation405Access.Read(
                    store,
                    context,
                    foreignTable,
                    1
                );
            }
            catch (InvalidOperationException)
            {
                foreignTableRejected = true;
            }
            EtsFoundation405Access.Require(foreignTableRejected);
            EtsFoundation405Access.Invoke(
                store,
                EtsFoundation405Names.Destroy,
                context,
                table
            );
            EtsFoundation405Access.Empty(store);

            return rejectedResource;
        });
        for (int n = 0; n < EtsFoundation405Names.CollectionAttempts && EtsFoundation405Access.Alive(partial); n++) EtsFoundation405Access.Collect();
        EtsFoundation405Access.Require(!EtsFoundation405Access.Alive(partial));
        EtsFoundation405Access.Require(runtime.Modules.Exports.Count == exports
            && !runtime.Modules.HasExport(EtsFoundation405Names.Ets, EtsFoundation405Names.New, 2));
    }

    internal static async Task IdentityAndOwnerDeath()
    {
        await using var runtime = new ProcessRuntime(1);
        object store = EtsFoundation405Access.Get(runtime);
        await using var reader = new RuntimeNodeFoundationHostActor(runtime);
        Term stale;
        await using (var owner = new RuntimeNodeFoundationHostActor(runtime))
        {
            stale = await owner.Invoke(context => EtsFoundation405Access.Create(store, context, true));
            long slot = await owner.Invoke(context => EtsFoundation405Access.Store(
                store,
                context,
                stale,
                Term.Tuple(Term.I(1))
            ));
            await owner.Invoke(context =>
            {
                bool duplicateRejected = false;
                try
                {
                    EtsFoundation405Access.Create(store, context, true);
                }
                catch (InvalidOperationException)
                {
                    duplicateRejected = true;
                }
                EtsFoundation405Access.Require(duplicateRejected);
                Term imported = runtime.ImportExternalTerm(ExternalTermFormat.Encode(stale)).Term;
                EtsFoundation405Access.Require(imported.Equals(stale)
                    && EtsFoundation405Access.Read(
                        store,
                        context,
                        imported,
                        slot
                    ).Equals(Term.Tuple(Term.I(1))));

                return true;
            });
            Term privateTable = await owner.Invoke(context => EtsFoundation405Access.Create(
                store,
                context,
                false,
                EtsFoundation405Names.Private
            ));
            long privateSlot = await owner.Invoke(context => EtsFoundation405Access.Store(
                store,
                context,
                privateTable,
                Term.Tuple(Term.I(1))
            ));
            Term publicTable = await owner.Invoke(context => EtsFoundation405Access.Create(
                store,
                context,
                false,
                EtsFoundation405Names.Public
            ));
            await reader.Invoke(context =>
            {
                EtsFoundation405Access.Require(EtsFoundation405Access.Read(
                    store,
                    context,
                    Term.A(EtsFoundation405Names.Name),
                    slot
                ).Equals(Term.Tuple(Term.I(1))));
                bool denied = false;
                try
                {
                    EtsFoundation405Access.Store(
                        store,
                        context,
                        stale,
                        Term.Tuple(Term.I(2))
                    );
                }
                catch (InvalidOperationException)
                {
                    denied = true;
                }
                EtsFoundation405Access.Require(denied);
                bool privateDenied = false;
                try
                {
                    EtsFoundation405Access.Read(
                        store,
                        context,
                        privateTable,
                        privateSlot
                    );
                }
                catch (InvalidOperationException)
                {
                    privateDenied = true;
                }
                EtsFoundation405Access.Require(privateDenied);
                long publicSlot = EtsFoundation405Access.Store(
                    store,
                    context,
                    publicTable,
                    Term.Tuple(Term.I(2))
                );
                EtsFoundation405Access.Require(EtsFoundation405Access.Read(
                    store,
                    context,
                    publicTable,
                    publicSlot
                ).Equals(Term.Tuple(Term.I(2))));

                return true;
            });
        }
        // The real process completion waited above, not a manual table cleanup call.
        EtsFoundation405Access.Empty(store);
        await reader.Invoke(context =>
        {
            Term replacement = EtsFoundation405Access.Create(
                store,
                context,
                true,
                EtsFoundation405Names.Private
            );
            EtsFoundation405Access.Require(!replacement.Equals(stale));
            bool staleRejected = false;
            try
            {
                EtsFoundation405Access.Read(
                    store,
                    context,
                    stale,
                    1
                );
            }
            catch (InvalidOperationException)
            {
                staleRejected = true;
            }
            EtsFoundation405Access.Require(staleRejected);
            EtsFoundation405Access.Invoke(
                store,
                EtsFoundation405Names.Destroy,
                context,
                replacement
            );

            return true;
        });
        EtsFoundation405Access.Empty(store);
        await reader.DisposeAsync();
        await runtime.DisposeAsync();
        EtsFoundation405Access.Empty(store, true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<ReferenceTerm> RejectAfterResourcePrefix(
        object store,
        ProcessContext context,
        Term table,
        Term failure
    )
    {
        ResourceReferenceTerm resource = context.Runtime.ReferenceResources.CreateBinary(new byte[] { 1, 2, 3 });
        bool rejected = false;
        try
        {
            EtsFoundation405Access.Store(
                store,
                context,
                table,
                Term.Tuple(resource, failure)
            );
        }
        catch (NotSupportedException error) when (error.Message == EtsFoundation405Names.UnsupportedFunction)
        {
            rejected = true;
        }
        EtsFoundation405Access.Require(rejected);

        return new(resource);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference<ReferenceTerm> Reference, long Slot) StoreResource(object store, ProcessContext context, Term table)
    {
        ResourceReferenceTerm resource = context.Runtime.ReferenceResources.CreateBinary(new byte[] { 1, 2, 3 });
        long slot = EtsFoundation405Access.Store(
            store,
            context,
            table,
            Term.Tuple(resource)
        );

        return (new(resource), slot);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ReadResource(object lease, ProcessRuntime runtime)
    {
        var tuple = (TupleTerm)EtsFoundation405Access.Invoke(lease, EtsFoundation405Names.Materialize, CancellationToken.None)!;
        using var resource = runtime.ReferenceResources.AcquireBinary((ReferenceTerm)tuple.Items[0]);

        return resource is not null && resource.Value.ToArray().SequenceEqual(new byte[] { 1, 2, 3 });
    }

    internal static async Task ResourceRetentionAndReadLease()
    {
        await using var runtime = new ProcessRuntime(1);
        object store = EtsFoundation405Access.Get(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term table = await actor.Invoke(context => EtsFoundation405Access.Create(store, context));
        var resource = await actor.Invoke(context => StoreResource(store, context, table));
        EtsFoundation405Access.Collect();
        EtsFoundation405Access.Require(EtsFoundation405Access.Alive(resource.Reference));
        object lease = await actor.Invoke(context => EtsFoundation405Access.Invoke(
            store,
            EtsFoundation405Names.AcquireRead,
            context,
            table,
            resource.Slot
        )!);
        await actor.Invoke(context =>
        {
            EtsFoundation405Access.Invoke(
                store,
                EtsFoundation405Names.Destroy,
                context,
                table
            );

            return true;
        });
        var snapshot = EtsFoundation405Access.Snapshot(store);
        EtsFoundation405Access.Require((int)snapshot[0]! == 0 && (long)snapshot[3]! == 1 && (long)snapshot[4]! > 0);
        EtsFoundation405Access.Require(ReadResource(lease, runtime));
        ((IDisposable)lease).Dispose();
        EtsFoundation405Access.Empty(store);
        for (int n = 0; n < EtsFoundation405Names.CollectionAttempts && EtsFoundation405Access.Alive(resource.Reference); n++) EtsFoundation405Access.Collect();
        EtsFoundation405Access.Require(!EtsFoundation405Access.Alive(resource.Reference));
    }

    internal static async Task CancellationCloseAndHalt()
    {
        await using (var runtime = new ProcessRuntime(1))
        await using (var actor = new RuntimeNodeFoundationHostActor(runtime))
        {
            object store = EtsFoundation405Access.Get(runtime);
            Term table = await actor.Invoke(context => EtsFoundation405Access.Create(store, context));
            var baseline = EtsFoundation405Access.Snapshot(store);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            bool captureCanceled = false;
            try
            {
                EtsFoundation405Access.CaptureCanceled(
                    store,
                    runtime,
                    Term.Tuple(Term.I(1)),
                    cancellation.Token
                );
            }
            catch (OperationCanceledException)
            {
                captureCanceled = true;
            }
            EtsFoundation405Access.Require(captureCanceled && baseline.Equals(EtsFoundation405Access.Snapshot(store)));
            var resource = await actor.Invoke(context => StoreResource(store, context, table));
            object lease = await actor.Invoke(context => EtsFoundation405Access.Invoke(
                store,
                EtsFoundation405Names.AcquireRead,
                context,
                table,
                resource.Slot
            )!);
            var beforeRead = EtsFoundation405Access.Snapshot(store);
            bool readCanceled = false;
            try
            {
                _ = EtsFoundation405Access.Invoke(lease, EtsFoundation405Names.Materialize, cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                readCanceled = true;
            }
            EtsFoundation405Access.Require(readCanceled && beforeRead.Equals(EtsFoundation405Access.Snapshot(store)) && ReadResource(lease, runtime));
            Term raceTable = await actor.Invoke(context => EtsFoundation405Access.Create(store, context));
            var raceResource = await actor.Invoke(context => StoreResource(store, context, raceTable));
            object raceLease = await actor.Invoke(context => EtsFoundation405Access.Invoke(
                store,
                EtsFoundation405Names.AcquireRead,
                context,
                raceTable,
                raceResource.Slot
            )!);
            await actor.Invoke(context =>
            {
                EtsFoundation405Access.Invoke(
                    store,
                    EtsFoundation405Names.Destroy,
                    context,
                    raceTable
                );

                return true;
            });
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<bool> materialize = Task.Run(async () =>
            {
                await start.Task;
                try
                {
                    return ReadResource(raceLease, runtime);
                }
                catch (ObjectDisposedException)
                {
                    return true;
                }
            });
            Task dispose = Task.Run(async () =>
            {
                await start.Task;
                ((IDisposable)raceLease).Dispose();
            });
            start.SetResult();
            await Task.WhenAll(materialize, dispose).WaitAsync(EtsFoundation405Names.Deadline);
            EtsFoundation405Access.Require(await materialize);
            EtsFoundation405Access.Invoke(store, EtsFoundation405Names.Close);
            await ((Task)EtsFoundation405Access.Invoke(store, EtsRetirement407Names.Drain)!);
            EtsFoundation405Access.Require((bool)EtsFoundation405Access.Snapshot(store)[5]! && (long)EtsFoundation405Access.Snapshot(store)[3]! == 1);
            await actor.Invoke(context =>
            {
                bool blocked = false;
                try
                {
                    EtsFoundation405Access.Create(store, context);
                }
                catch (ObjectDisposedException)
                {
                    blocked = true;
                }
                EtsFoundation405Access.Require(blocked);

                return true;
            });
            EtsFoundation405Access.Require(ReadResource(lease, runtime));
            ((IDisposable)lease).Dispose();
            ((IDisposable)lease).Dispose();
            EtsFoundation405Access.Empty(store, true);
        }
        await using var halted = new ProcessRuntime(1);
        object haltStore = EtsFoundation405Access.Get(halted);
        await using var haltActor = new RuntimeNodeFoundationHostActor(halted);
        await haltActor.Invoke(context =>
        {
            Term table = EtsFoundation405Access.Create(haltStore, context, true);
            EtsFoundation405Access.Store(
                haltStore,
                context,
                table,
                Term.Tuple(Term.I(1))
            );

            return true;
        });
        await halted.RequestHalt(0, new RuntimeHaltOptions(false)).WaitAsync(EtsFoundation405Names.Deadline);
        await halted.HaltQuiescence.WaitAsync(EtsFoundation405Names.Deadline);
        EtsFoundation405Access.Empty(haltStore, true);
    }
}
