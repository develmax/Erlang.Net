using System.Runtime.ExceptionServices;
using static Erlang.Tests.AtomicsEtsAggregate594Names;
using static Erlang.Tests.NativeResource524Access;

namespace Erlang.Tests;

internal static class AtomicsEtsAggregate594Checks
{
    internal static Task LaterRowFailure() => AtomicsP2a586Checks.Run(async fixture =>
    {
        var failures = new List<Exception>();
        var leases = new List<IDisposable>(RowCount);
        RuntimeNodeFoundationHostActor? actor = null;
        IDisposable? frame = null, producer = null, snapshot = null, probeCarrier = null;
        object? store = null, savedNode = null;
        Array? nodes = null;
        Term? table = null, source = null, firstPair = null;
        bool corrupted = false;
        try
        {
            producer = fixture.Lease(AtomicsP2a586Names.ProducerKind);
            actor = new(fixture.Runtime);
            ProcessContext context = await actor.Ready.WaitAsync(AtomicsMagic586Names.Deadline);
            object heap = Property(context, AtomicsMagic586Names.Heap);
            frame = (IDisposable)Call(heap, AtomicsMagic586Names.Enter)!;
            int slot = (int)Call(frame, AtomicsMagic586Names.Reserve)!;
            Call(
                frame,
                AtomicsMagic586Names.Set,
                slot,
                fixture.Reference
            );
            source = (Term)Call(frame, AtomicsMagic586Names.Read, slot)!;
            store = EtsFoundation405Access.Get(fixture.Runtime);
            object actualStore = store;
            table = await actor.Invoke(c => EtsFoundation405Access.Create(actualStore, c));
            Term actualTable = table;
            foreach (Term key in new[] { Term.A(KeyA), Term.A(KeyB) })
            {
                long id = await actor.Invoke(c => EtsFoundation405Access.Store(
                    actualStore,
                    c,
                    actualTable,
                    Term.Tuple(key, source!)
                ));
                leases.Add(await actor.Invoke(c => (IDisposable)EtsFoundation405Access.Invoke(
                    actualStore,
                    EtsFoundation405Names.AcquireRead,
                    c,
                    actualTable,
                    id
                )!));
            }
            Array typed = Array.CreateInstance(leases[0].GetType(), RowCount);
            for (int index = 0; index < leases.Count; index++) typed.SetValue(leases[index], index);
            snapshot = (IDisposable)Activator.CreateInstance(
                typeof(ProcessRuntime).Assembly.GetType(SnapshotType, true)!,
                Members,
                null,
                [typed],
                null
            )!;
            firstPair = await actor.Invoke(_ => (Term)Call(leases[0], Materialize, CancellationToken.None)!);
            probeCarrier = (IDisposable?)typeof(ResourceReferenceTerm).GetProperty(HostCarrier, Members)!.GetValue(((TupleTerm)firstPair).Items[1]);
            Check(probeCarrier is not null && Hosts(fixture.Array) == OneHost, failures);
            probeCarrier?.Dispose();
            probeCarrier = null;
            fixture.ReleaseProducer();
            producer.Dispose();
            producer = null;
            Call(frame, AtomicsMagic586Names.Clear, slot);
            Call(heap, AtomicsMagic586Names.Collect);
            object payload = Field(Field(leases[1], OwnerField), PayloadField);
            nodes = (Array)Field(payload, NodesField);
            savedNode = nodes.GetValue(RootNode)!;
            // GetValue boxes the readonly record twice: preserve the original
            // box for restoration, modify only the distinct corruption copy.
            object malformed = nodes.GetValue(RootNode)!;
            var kind = malformed.GetType().GetProperty(KindProperty)!;
            kind.SetValue(malformed, Enum.ToObject(kind.PropertyType, InvalidKind));
            corrupted = true;
            nodes.SetValue(malformed, RootNode);
            bool rejected = false;
            try
            {
                await actor.Invoke(_ => Call(snapshot!, Materialize, CancellationToken.None));
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            long after = Hosts(fixture.Array);
            Console.WriteLine(Observation, after);
            Check(rejected && after == NoHosts, failures);
            nodes.SetValue(savedNode, RootNode);
            corrupted = false;
            snapshot.Dispose();
            snapshot = null;
            foreach (IDisposable lease in leases) lease.Dispose();
            await actor.Invoke(c => EtsFoundation405Access.Invoke(
                actualStore,
                EtsFoundation405Names.Destroy,
                c,
                actualTable
            ));
            table = null;
            try
            {
                await ((Task)Property(fixture.Array, AtomicsP2a586Names.Retired)).WaitAsync(AtomicsMagic586Names.Deadline);
                Check(!(bool)Property(fixture.Array, AtomicsP2a586Names.Allocated), failures);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            GC.KeepAlive(source);
            GC.KeepAlive(firstPair);
            GC.KeepAlive(fixture.Reference);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            if (corrupted && nodes is not null)
                try
                {
                    nodes.SetValue(savedNode, RootNode);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            try
            {
                probeCarrier?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                snapshot?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            foreach (IDisposable lease in leases)
                try
                {
                    lease.Dispose();
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (actor is not null && store is not null && table is not null)
                try
                {
                    await actor.Invoke(c => EtsFoundation405Access.Invoke(
                        store,
                        EtsFoundation405Names.Destroy,
                        c,
                        table
                    ));
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
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(AtomicsMagic586Names.Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            GC.KeepAlive(source);
            GC.KeepAlive(firstPair);
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(CleanupFailure, failures);
    });

    private static object Field(object value, string name) => value.GetType().GetField(name, Members)!.GetValue(value)!;

    private static long Hosts(object array)
    {
        lock (Field(array, ArrayGateField))
            return (long)Field(array, HostRootsField);
    }

    private static void Check(bool value, List<Exception> failures)
    {
        if (!value) failures.Add(new InvalidOperationException(Failure));
    }
}
