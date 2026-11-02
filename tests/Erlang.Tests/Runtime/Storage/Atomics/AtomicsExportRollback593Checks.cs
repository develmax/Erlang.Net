using System.Runtime.ExceptionServices;
using static Erlang.Tests.AtomicsExportRollback593Names;
using static Erlang.Tests.NativeResource524Access;

namespace Erlang.Tests;

internal static class AtomicsExportRollback593Checks
{
    internal static Task LateFailureReleasesActualBacking() => Run(false);

    internal static Task SuccessfulCopyTransfersToWrappers() => Run(true);

    private static Task Run(bool success) => AtomicsP2a586Checks.Run(async fixture =>
    {
        var failures = new List<Exception>();
        var carriers = new List<IDisposable>(ExportOccurrences);
        RuntimeNodeFoundationHostActor? actor = null;
        IDisposable? frame = null, producer = null;
        Term? graph = null, escaped = null;
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
            var source = (ReferenceTerm)Call(frame, AtomicsMagic586Names.Read, slot)!;
            long hostsAtClosure = UnobservedHosts;
            FunctionTerm? unportable = success ? null : await RuntimeFunctionMarker364Checks.CreateRefused(
                fixture.Runtime,
                actor,
                heap,
                frame
            );
            graph = Term.List(source, success ? (Term)source : unportable!);
            if (success)
            {
                escaped = (Term)Call(
                    heap,
                    Export,
                    frame,
                    graph
                )!;
                foreach (Term leaf in Cons.Items(escaped))
                    carriers.Add((IDisposable)typeof(ResourceReferenceTerm).GetProperty(HostCarrier, Members)!.GetValue(leaf)!);
                Check(carriers.Count == ExportOccurrences && HostRoots(fixture.Array) == ExportOccurrences, failures);
            }
            else
            {
                bool rejected = false;
                Task<Term> pending;
                bool observed;
                object gate = fixture.Runtime.Functions.GetType().GetField(RuntimeFunctionMarker364Names.FunctionGate, Members)!.GetValue(fixture.Runtime.Functions)!;
                // Hold the real receiver registry until partial Host custody is
                // visible; the later genuine function recipe then refuses.
                lock (gate)
                {
                    pending = actor.Invoke(context => (Term)Call(
                        heap,
                        Export,
                        frame,
                        graph
                    )!);
                    observed = SpinWait.SpinUntil(() => HostRoots(fixture.Array) == FailedCopyOccurrences || pending.IsCompleted, AtomicsMagic586Names.Deadline);
                    hostsAtClosure = HostRoots(fixture.Array);
                }
                try
                {
                    _ = await pending.WaitAsync(AtomicsMagic586Names.Deadline);
                }
                catch (NotSupportedException error) when (error.Message == RuntimeFunctionMarker364Names.Refused)
                {
                    rejected = true;
                }
                Check(observed, failures);
                long after = HostRoots(fixture.Array);
                Console.WriteLine(Observation, hostsAtClosure, after);
                Check(rejected && hostsAtClosure == FailedCopyOccurrences, failures);
                Check(after == HostsAfterRollback, failures);
            }
            fixture.ReleaseProducer();
            producer.Dispose();
            producer = null;
            Call(frame, AtomicsMagic586Names.Clear, slot);
            Call(heap, AtomicsMagic586Names.Collect);
            if (success)
            {
                carriers[0].Dispose();
                Check(HostRoots(fixture.Array) == HostsAfterFirstRelease && (bool)Property(fixture.Array, AtomicsP2a586Names.Allocated), failures);
                using (fixture.Pin())
                    Check((long)Call(fixture.Array, AtomicsP2a586Names.Cell, FirstCell)! == InitialCellValue, failures);
                carriers[1].Dispose();
            }
            // Even the failure case continues through actual collection and
            // physical retirement instead of stopping at a private counter.
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
            GC.KeepAlive(graph);
            GC.KeepAlive(escaped);
            GC.KeepAlive(fixture.Reference);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            foreach (IDisposable carrier in carriers)
                try
                {
                    carrier.Dispose();
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
            GC.KeepAlive(graph);
            GC.KeepAlive(escaped);
        }
        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(CleanupFailure, failures);
    });

    private static long HostRoots(object array)
    {
        object gate = array.GetType().GetField(ArrayGateField, Members)!.GetValue(array)!;
        lock (gate)
            return (long)array.GetType().GetField(HostRootsField, Members)!.GetValue(array)!;
    }

    private static void Check(bool condition, List<Exception> failures)
    {
        if (!condition)
            failures.Add(new InvalidOperationException(Failure));
    }
}
