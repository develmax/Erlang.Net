using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.CountersStore596Names;
using static Erlang.Tests.NativeResource524Access;

namespace Erlang.Tests;

// Storage-domain controls. They do not assert that opaque Erlang dispatch
// already admits a borrowed owned wrapper into these direct store APIs.
internal static class CountersStore596Checks
{
    internal static Task QueueHandoff() => Run(0);

    internal static Task EtsHandoff() => Run(1);

    internal static Task PersistentHandoff() => Run(2);

    private static Task Run(int route) => CountersCustody596Checks.Run(async fixture =>
    {
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? sender = null, receiver = null;
        IDisposable? sourceFrame = null, destinationFrame = null, producer = null, transportCarrier = null, returnedCarrier = null;
        Func<Task>? removeStore = null;
        try
        {
            producer = fixture.Lease(CountersCustody596Names.ProducerKind);
            sender = new(fixture.Runtime);
            receiver = new(fixture.Runtime);
            ProcessContext sourceContext = await sender.Ready.WaitAsync(CountersMagic596Names.Deadline);
            ProcessContext destinationContext = await receiver.Ready.WaitAsync(CountersMagic596Names.Deadline);
            object sourceHeap = Property(sourceContext, CountersMagic596Names.Heap);
            object destinationHeap = Property(destinationContext, CountersMagic596Names.Heap);
            sourceFrame = (IDisposable)Call(sourceHeap, CountersMagic596Names.Enter)!;
            destinationFrame = (IDisposable)Call(destinationHeap, CountersMagic596Names.Enter)!;
            int sourceSlot = (int)Call(sourceFrame, CountersMagic596Names.Reserve)!;
            int destinationSlot = (int)Call(destinationFrame, CountersMagic596Names.Reserve)!;
            Call(
                sourceFrame,
                CountersMagic596Names.Set,
                sourceSlot,
                fixture.Reference
            );
            var source = (ReferenceTerm)Call(sourceFrame, CountersMagic596Names.Read, sourceSlot)!;
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
                        CountersMagic596Names.Deadline))!);
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
            Call(sourceFrame, CountersMagic596Names.Clear, sourceSlot);
            Call(sourceHeap, CountersMagic596Names.Collect);
            // The source header is retired BEFORE the destination reads.
            // A store must retain backing plus a valid reconstructable identity,
            // not merely the old source wrapper whose words are now stale.
            Term restored = await read();
            if (restored is ResourceReferenceTerm restoredReference)
                returnedCarrier = typeof(ResourceReferenceTerm).GetProperty(HostCarrier, Members)!.GetValue(restoredReference) as IDisposable;
            Call(
                destinationFrame,
                CountersMagic596Names.Set,
                destinationSlot,
                restored
            );
            var destination = (ReferenceTerm)Call(destinationFrame, CountersMagic596Names.Read, destinationSlot)!;
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
            Require((bool)Property(fixture.Array, CountersCustody596Names.Allocated));
            Call(destinationFrame, CountersMagic596Names.Clear, destinationSlot);
            Call(destinationHeap, CountersMagic596Names.Collect);
            await ((Task)Property(fixture.Array, CountersCustody596Names.Retired)).WaitAsync(CountersMagic596Names.Deadline);
            Require(!(bool)Property(fixture.Array, CountersCustody596Names.Allocated));
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
                    await sender.DisposeAsync().AsTask().WaitAsync(CountersMagic596Names.Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (receiver is not null)
                try
                {
                    await receiver.DisposeAsync().AsTask().WaitAsync(CountersMagic596Names.Deadline);
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
