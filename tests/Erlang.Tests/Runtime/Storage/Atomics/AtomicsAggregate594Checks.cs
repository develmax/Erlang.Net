using System.Buffers.Binary;
using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using static Erlang.Tests.AtomicsAggregate594Names;
using static Erlang.Tests.NativeResource524Access;

namespace Erlang.Tests;

internal static class AtomicsAggregate594Checks
{
    internal static Task GetAllLaterAreaFailure() => Run(GetAllRoute);

    internal static Task SnapshotLaterAreaFailure() => Run(SnapshotRoute);

    internal static Task PairsLaterAreaFailure() => Run(PairsRoute);

    private static Task Run(int route) => AtomicsP2a586Checks.Run(async fixture =>
    {
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? sender = null, receiver = null;
        IDisposable? frame = null, producer = null, snapshot = null, wrapper = null, probeCarrier = null, transaction = null;
        object? store = null, arena = null;
        bool storedA = false, storedB = false, corrupted = false;
        nint pointer = 0;
        long savedMagic = 0;
        Term? source = null, firstPair = null;
        Term keyA = Term.A(KeyA), keyB = Term.A(KeyB);
        try
        {
            producer = fixture.Lease(AtomicsP2a586Names.ProducerKind);
            sender = new(fixture.Runtime);
            receiver = new(fixture.Runtime);
            ProcessContext context = await sender.Ready.WaitAsync(AtomicsMagic586Names.Deadline);
            await receiver.Ready.WaitAsync(AtomicsMagic586Names.Deadline);
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
            store = PersistentTerm408HostAccess.Store(fixture.Runtime);
            arena = Field(store, ArenaField);
            object actualStore = store;
            await sender.InvokeAsync(c => PersistentTerm408HostAccess.Put(
                actualStore,
                c,
                keyA,
                source!
            ));
            storedA = true;
            await sender.InvokeAsync(c => PersistentTerm408HostAccess.Put(
                actualStore,
                c,
                keyB,
                source!
            ));
            storedB = true;
            snapshot = (IDisposable)PersistentArena416HostAccess.Snapshot(arena);
            object actualSnapshot = snapshot;
            object[] ordered = OrderedAreas(actualSnapshot);
            Check(ordered.Length == 2, failures);
            // A real successful first-area output is observed, then released.
            // The retained snapshot pins BOTH actual native arena blocks.
            firstPair = await receiver.Invoke(_ => (Term)Call(ordered[FirstArea], Materialize, CancellationToken.None)!);
            var first = (ResourceReferenceTerm)((TupleTerm)firstPair).Items[1];
            probeCarrier = (IDisposable?)typeof(ResourceReferenceTerm).GetProperty(HostCarrier, Members)!.GetValue(first);
            Check(probeCarrier is not null && Hosts(fixture.Array) == OneHost, failures);
            probeCarrier?.Dispose();
            probeCarrier = null;
            fixture.ReleaseProducer();
            producer.Dispose();
            producer = null;
            Call(frame, AtomicsMagic586Names.Clear, slot);
            Call(heap, AtomicsMagic586Names.Collect);
            var secondOwner = (SafeHandle)Field(ordered[SecondArea], PairOwnerField);
            pointer = secondOwner.DangerousGetHandle();
            if (pointer == 0 || secondOwner.IsInvalid)
                throw new InvalidOperationException(Failure);
            savedMagic = Marshal.ReadInt64(pointer, MagicOffset);
            // Exclusive fixture: no store writes or competing materializers
            // exist until this bounded operation completes and bytes restore.
            corrupted = true;
            Marshal.WriteInt64(pointer, MagicOffset, savedMagic ^ CorruptionBit);
            if (route == SnapshotRoute)
                wrapper = (IDisposable)Activator.CreateInstance(
                    typeof(ProcessRuntime).Assembly.GetType(SnapshotType, true)!,
                    Members,
                    null,
                    [actualSnapshot],
                    null
                )!;
            bool rejected = false;
            try
            {
                await receiver.Invoke(c => route switch
                {
                    GetAllRoute => PersistentTerm408HostAccess.Call(actualStore, PersistentTerm408HostNames.GetAll, c),
                    SnapshotRoute => Call(wrapper!, Materialize, CancellationToken.None),
                    _ => Call(actualSnapshot, PersistentArena416HostNames.Pairs, CancellationToken.None)
                });
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }
            long after = Hosts(fixture.Array);
            Console.WriteLine(Observation, route, after);
            Check(rejected && after == NoHosts, failures);
            // With the new explicit helper, prove a first output is REALLY
            // tracked before the second-area exception, without a test hook.
            // The old-baseline primary assertions above are unconditional.
            MethodInfo? selected = actualSnapshot.GetType().GetMethod(PairsSelected, Members);
            if (selected is not null)
            {
                transaction = (IDisposable)Activator.CreateInstance(typeof(ProcessRuntime).Assembly.GetType(TransactionType, true)!, true)!;
                bool selectedRejected = false;
                try
                {
                    Call(
                        actualSnapshot,
                        PairsSelected,
                        CancellationToken.None,
                        transaction
                    );
                }
                catch (InvalidDataException)
                {
                    selectedRejected = true;
                }
                Check(
                    selectedRejected && ((ICollection)Field(transaction, TrackedLeasesField)).Count == OneHost
                    && Hosts(fixture.Array) == OneHost,
                    failures
                );
                transaction.Dispose();
                transaction = null;
                Check(Hosts(fixture.Array) == NoHosts, failures);
            }
            Marshal.WriteInt64(pointer, MagicOffset, savedMagic);
            corrupted = false;
            wrapper?.Dispose();
            wrapper = null;
            snapshot.Dispose();
            snapshot = null;
            await sender.InvokeAsync(c => ((ValueTask<Term>)PersistentTerm408HostAccess.Call(
                actualStore,
                PersistentTerm408HostNames.Erase,
                c,
                keyA
            )!).AsTask());
            storedA = false;
            await sender.InvokeAsync(c => ((ValueTask<Term>)PersistentTerm408HostAccess.Call(
                actualStore,
                PersistentTerm408HostNames.Erase,
                c,
                keyB
            )!).AsTask());
            storedB = false;
            await PersistentArena416HostAccess.Reclaim(arena).WaitAsync(AtomicsMagic586Names.Deadline);
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
            if (corrupted)
                try
                {
                    Marshal.WriteInt64(pointer, MagicOffset, savedMagic);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            try
            {
                transaction?.Dispose();
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
                wrapper?.Dispose();
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
            if (sender is not null && store is not null)
            {
                object remaining = store;
                foreach (Term key in new[] { storedA ? keyA : null, storedB ? keyB : null }.OfType<Term>())
                    try
                    {
                        await sender.InvokeAsync(c => ((ValueTask<Term>)PersistentTerm408HostAccess.Call(
                            remaining,
                            PersistentTerm408HostNames.Erase,
                            c,
                            key
                        )!).AsTask());
                    }
                    catch (Exception error)
                    {
                        failures.Add(error);
                    }
            }
            if (arena is not null)
                try
                {
                    await PersistentArena416HostAccess.Reclaim(arena).WaitAsync(AtomicsMagic586Names.Deadline);
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
            GC.KeepAlive(source);
            GC.KeepAlive(firstPair);
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(CleanupFailure, failures);
    });

    private static object[] OrderedAreas(object snapshot)
    {
        byte[] image = (byte[])Field(snapshot, ImageField);
        Array areas = (Array)Field(snapshot, AreasField);
        Type names = typeof(ProcessRuntime).Assembly.GetType(TableNamesType, true)!;
        int capacityOffset = (int)names.GetField(CapacityOffset, Members)!.GetRawConstantValue()!;
        int capacity = BinaryPrimitives.ReadInt32LittleEndian(image.AsSpan(capacityOffset));
        MethodInfo root = typeof(ProcessRuntime).Assembly.GetType(TableType, true)!.GetMethod(RootIndex, Members)!;

        return Enumerable.Range(0, capacity).Select(slot => (int)root.Invoke(null, [image, slot])!)
            .Where(index => index != 0).Select(index => areas.GetValue(index - 1)!).ToArray();
    }

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
