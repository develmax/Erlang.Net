using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang;
using static Erlang.Tests.AtomicsPersistentSelection591Names;
using static Erlang.Tests.NativeResource524Access;

namespace Erlang.Tests;

internal static class AtomicsPersistentSelection591Checks
{
    internal static Task UnusedKeyDoesNotEscape() => Run(false);

    internal static Task EscapingAliasRetainsBacking() => Run(true);

    private static Task Run(bool alias) => AtomicsP2a586Checks.Run(async fixture =>
    {
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? sender = null, receiver = null;
        IDisposable? frame = null, producer = null, returnedCarrier = null;
        object? store = null;
        bool stored = false;
        try
        {
            producer = fixture.Lease(AtomicsP2a586Names.ProducerKind);
            sender = new(fixture.Runtime);
            receiver = new(fixture.Runtime);
            ProcessContext sourceContext = await sender.Ready.WaitAsync(AtomicsMagic586Names.Deadline);
            _ = await receiver.Ready.WaitAsync(AtomicsMagic586Names.Deadline);
            object heap = Property(sourceContext, AtomicsMagic586Names.Heap);
            frame = (IDisposable)Call(heap, AtomicsMagic586Names.Enter)!;
            int slot = (int)Call(frame, AtomicsMagic586Names.Reserve)!;
            Call(
                frame,
                AtomicsMagic586Names.Set,
                slot,
                fixture.Reference
            );
            var source = (ReferenceTerm)Call(frame, AtomicsMagic586Names.Read, slot)!;
            Term value = alias ? Term.Tuple(source, source) : Term.I(Value);
            store = PersistentTerm408HostAccess.Store(fixture.Runtime);
            object actualStore = store;
            await sender.InvokeAsync(context => PersistentTerm408HostAccess.Put(
                actualStore,
                context,
                source,
                value
            ));
            stored = true;
            fixture.ReleaseProducer();
            producer.Dispose();
            producer = null;
            Call(frame, AtomicsMagic586Names.Clear, slot);
            Call(heap, AtomicsMagic586Names.Collect);
            // The lookup identity is portable; the table's explicit store lease
            // is now the real owner, independent of the stale CLR source.
            Term result = await receiver.Invoke(context => (Term)PersistentTerm408HostAccess.Call(
                actualStore,
                PersistentTerm408HostNames.Get,
                context,
                fixture.Reference
            )!);
            ResourceReferenceTerm? output = null;
            if (alias)
            {
                Check(result is TupleTerm { Items.Count: 2 });
                var pair = (TupleTerm)result;
                Check(ReferenceEquals(pair.Items[0], pair.Items[1]));
                output = pair.Items[0] as ResourceReferenceTerm;
                Check(output is not null);
                returnedCarrier = typeof(ResourceReferenceTerm).GetProperty(HostCarrier, Members)!.GetValue(output) as IDisposable;
                Check(returnedCarrier is not null);
            }
            else
                Check(result.Equals(Term.I(Value)));
            await sender.InvokeAsync(context => ((ValueTask<Term>)PersistentTerm408HostAccess.Call(
                actualStore,
                PersistentTerm408HostNames.Erase,
                context,
                fixture.Reference
            )!).AsTask());
            stored = false;
            if (output is not null)
            {
                // The source header AND table are gone. A selected output must
                // still be a usable actual native backing, not just equal words.
                ErlangFunction put = Native(fixture.Runtime, AtomicsHost413Names.Put, 3);
                ErlangFunction get = Native(fixture.Runtime, AtomicsHost413Names.Get, 2);
                await receiver.InvokeAsync(async context => await put(context, [output, Term.I(1), Term.I(Value)]));
                Term actual = await receiver.InvokeAsync(async context => await get(context, [output, Term.I(1)]));
                Check(actual.Equals(Term.I(Value)));
            }
            returnedCarrier?.Dispose();
            returnedCarrier = null;
            await ((Task)Property(fixture.Array, AtomicsP2a586Names.Retired)).WaitAsync(AtomicsMagic586Names.Deadline);
            Check(!(bool)Property(fixture.Array, AtomicsP2a586Names.Allocated));
            GC.KeepAlive(source);
            GC.KeepAlive(result);
            GC.KeepAlive(fixture.Reference);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            if (stored && sender is not null && store is not null)
            {
                object remainingStore = store;
                try
                {
                    await sender.InvokeAsync(context => ((ValueTask<Term>)PersistentTerm408HostAccess.Call(
                        remainingStore,
                        PersistentTerm408HostNames.Erase,
                        context,
                        fixture.Reference
                    )!).AsTask())
                        .WaitAsync(AtomicsMagic586Names.Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            }
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
                frame?.Dispose();
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
        object?[] args = [runtime, GuestModule, function, arity, null];
        Check((bool)typeof(ProcessRuntime).Assembly.GetType(NativeType, true)!.GetMethod(Capture, Members)!.Invoke(null, args)!);

        return (ErlangFunction)args[4]!;
    }

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }
}
