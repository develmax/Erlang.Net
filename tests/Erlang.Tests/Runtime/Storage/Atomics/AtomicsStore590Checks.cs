using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.AtomicsStore590Names;
using static Erlang.Tests.NativeResource524Access;

namespace Erlang.Tests;

// Storage-domain controls. They do not assert that opaque Erlang dispatch
// already admits a borrowed owned wrapper into these direct store APIs.
internal static class AtomicsStore590Checks
{
    internal static Task QueueHandoff() => Run(0);

    internal static Task EtsHandoff() => Run(1);

    internal static Task PersistentHandoff() => Run(2);

    private static Task Run(int route) => AtomicsP2a586Checks.Run(async fixture =>
    {
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? sender = null, receiver = null;
        IDisposable? sourceFrame = null, destinationFrame = null, producer = null, transportCarrier = null, returnedCarrier = null;
        Func<Task>? removeStore = null;
        try
        {
            producer = fixture.Lease(AtomicsP2a586Names.ProducerKind);
            sender = new(fixture.Runtime);
            receiver = new(fixture.Runtime);
            ProcessContext sourceContext = await sender.Ready.WaitAsync(AtomicsMagic586Names.Deadline);
            ProcessContext destinationContext = await receiver.Ready.WaitAsync(AtomicsMagic586Names.Deadline);
            object sourceHeap = Property(sourceContext, AtomicsMagic586Names.Heap);
            object destinationHeap = Property(destinationContext, AtomicsMagic586Names.Heap);
            sourceFrame = (IDisposable)Call(sourceHeap, AtomicsMagic586Names.Enter)!;
            destinationFrame = (IDisposable)Call(destinationHeap, AtomicsMagic586Names.Enter)!;
            int sourceSlot = (int)Call(sourceFrame, AtomicsMagic586Names.Reserve)!;
            int destinationSlot = (int)Call(destinationFrame, AtomicsMagic586Names.Reserve)!;
            Call(
                sourceFrame,
                AtomicsMagic586Names.Set,
                sourceSlot,
                fixture.Reference
            );
            var source = (ReferenceTerm)Call(sourceFrame, AtomicsMagic586Names.Read, sourceSlot)!;
            Term key = Term.A(Key);
            Func<Task<Term>> read;
            if (route == 0)
            {
                // The same authoritative export used by guest send happens
                // before runtime/mailbox gates. Keep its carrier through receive.
                var transported = (ReferenceTerm)Call(
                    sourceHeap,
                    Export,
                    sourceFrame,
                    source
                )!;
                transportCarrier = typeof(ResourceReferenceTerm).GetProperty(HostCarrier, Members)!.GetValue(transported) as IDisposable;
                fixture.Runtime.Send(destinationContext.Self, transported);
                read = () => receiver.InvokeAsync(async context =>
                    (Term)(await context.ReceiveAsync(value => value is ResourceReferenceTerm ? value : null,
                        AtomicsMagic586Names.Deadline))!);
            }
            else if (route == 1)
            {
                object store = EtsFoundation405Access.Get(fixture.Runtime);
                Term table = await sender.Invoke(context => EtsFoundation405Access.Create(store, context));
                removeStore = async () =>
                {
                    await sender.Invoke(context => EtsFoundation405Access.Invoke(
                        store,
                        EtsFoundation405Names.Destroy,
                        context,
                        table
                    ));
                };
                long slot = await sender.Invoke(context => EtsFoundation405Access.Store(
                    store,
                    context,
                    table,
                    Term.Tuple(key, source)
                ));
                read = () => receiver.Invoke(context => ((TupleTerm)EtsFoundation405Access.Read(
                    store,
                    context,
                    table,
                    slot
                )).Items[1]);
            }
            else
            {
                object store = PersistentTerm408HostAccess.Store(fixture.Runtime);
                removeStore = async () =>
                {
                    await sender.InvokeAsync(context => ((ValueTask<Term>)PersistentTerm408HostAccess.Call(
                        store,
                        PersistentTerm408HostNames.Erase,
                        context,
                        key
                    )!).AsTask());
                };
                await sender.InvokeAsync(context => PersistentTerm408HostAccess.Put(
                    store,
                    context,
                    key,
                    source
                ));
                read = () => receiver.Invoke(context => (Term)PersistentTerm408HostAccess.Call(
                    store,
                    PersistentTerm408HostNames.Get,
                    context,
                    key
                )!);
            }
            fixture.ReleaseProducer();
            producer.Dispose();
            producer = null;
            Call(sourceFrame, AtomicsMagic586Names.Clear, sourceSlot);
            Call(sourceHeap, AtomicsMagic586Names.Collect);
            // The source header is retired BEFORE the destination reads.
            // A store must retain backing plus a valid reconstructable identity,
            // not merely the old source wrapper whose words are now stale.
            Term restored = await read();
            if (restored is ResourceReferenceTerm restoredReference)
                returnedCarrier = typeof(ResourceReferenceTerm).GetProperty(HostCarrier, Members)!.GetValue(restoredReference) as IDisposable;
            Call(
                destinationFrame,
                AtomicsMagic586Names.Set,
                destinationSlot,
                restored
            );
            var destination = (ReferenceTerm)Call(destinationFrame, AtomicsMagic586Names.Read, destinationSlot)!;
            if (removeStore is not null)
            {
                await removeStore();
                removeStore = null;
            }
            // A public store read can return its own genuine host carrier.
            // The destination root is already published before that custody drops.
            if (!ReferenceEquals(returnedCarrier, transportCarrier))
                returnedCarrier?.Dispose();
            returnedCarrier = null;
            transportCarrier?.Dispose();
            transportCarrier = null;
            ErlangFunction put = Native(fixture.Runtime, Put, 3), get = Native(fixture.Runtime, Get, 2);
            await receiver.InvokeAsync(async context => await put(context, [destination, Term.I(1), Term.I(ExpectedValue)]));
            Term actual = await receiver.InvokeAsync(async context => await get(context, [destination, Term.I(1)]));
            Require(actual.Equals(Term.I(ExpectedValue)));
            Require((bool)Property(fixture.Array, AtomicsP2a586Names.Allocated));
            Call(destinationFrame, AtomicsMagic586Names.Clear, destinationSlot);
            Call(destinationHeap, AtomicsMagic586Names.Collect);
            await ((Task)Property(fixture.Array, AtomicsP2a586Names.Retired)).WaitAsync(AtomicsMagic586Names.Deadline);
            Require(!(bool)Property(fixture.Array, AtomicsP2a586Names.Allocated));
            GC.KeepAlive(source);
            GC.KeepAlive(restored);
            GC.KeepAlive(destination);
            GC.KeepAlive(fixture.Reference);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            if (removeStore is not null)
                try
                {
                    await removeStore();
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (!ReferenceEquals(returnedCarrier, transportCarrier))
                try
                {
                    returnedCarrier?.Dispose();
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            try
            {
                transportCarrier?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                sourceFrame?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                destinationFrame?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                producer?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            if (sender is not null)
                try
                {
                    await sender.DisposeAsync().AsTask().WaitAsync(AtomicsMagic586Names.Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (receiver is not null)
                try
                {
                    await receiver.DisposeAsync().AsTask().WaitAsync(AtomicsMagic586Names.Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
        }
        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(CleanupFailure, failures);
    });

    private static ErlangFunction Native(ProcessRuntime runtime, string function, int arity)
    {
        object?[] arguments = [runtime, GuestModule, function, arity, null];
        Require((bool)typeof(ProcessRuntime).Assembly.GetType(NativeType, true)!.GetMethod(Capture, Members)!.Invoke(null, arguments)!);

        return (ErlangFunction)arguments[4]!;
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }
}
