using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.AtomicsDictionaryHost589Names;
using static Erlang.Tests.NativeResource524Access;

namespace Erlang.Tests;

internal static class AtomicsDictionaryHost589Checks
{
    internal static Task GetterSurvivesEraseAndCollection() => Run(false);

    internal static Task SnapshotSurvivesEraseAndCollection() => Run(true);

    private static async Task Run(bool snapshot)
    {
        var failures = new List<Exception>();
        AtomicsP2a586Fixture? fixture = null;
        RuntimeNodeFoundationHostActor? actor = null;
        IDisposable? producer = null;
        IDisposable? carrier = null;
        ReferenceTerm? exported = null;
        try
        {
            fixture = new();
            // Initializes the actual array but keeps it provisionally owned
            // until the public dictionary has committed its genuine root.
            producer = fixture.Lease(AtomicsP2a586Names.ProducerKind);
            actor = new(fixture.Runtime);
            ProcessContext context = await actor.Ready.WaitAsync(AtomicsMagic586Names.Deadline);
            object heap = Property(context, AtomicsMagic586Names.Heap);
            Term key = Term.A(Key);
            await actor.Invoke(c =>
            {
                c.Dictionary[key] = fixture.Reference;

                return true;
            });
            fixture.ReleaseProducer();
            producer.Dispose();
            producer = null;
            // Both APIs return to an ordinary .NET caller through the real
            // public facade, without ModuleRegistry applying an extra export.
            exported = await actor.Invoke(c => snapshot
                ? (ReferenceTerm)c.Dictionary.ToArray().Single(pair => pair.Key.Equals(key)).Value
                : (ReferenceTerm)c.Dictionary[key]);
            carrier = typeof(ResourceReferenceTerm).GetProperty(HostCarrier, Members)!
                .GetValue(exported) as IDisposable;
            await actor.Invoke(c =>
            {
                c.Dictionary.Clear();
                Call(heap, AtomicsMagic586Names.Collect);

                return true;
            });
            // This is the actual validated native body, not a carrier-field
            // presence assertion or an estimated resource count.
            ErlangFunction put = Native(fixture.Runtime, Put, 3);
            ErlangFunction get = Native(fixture.Runtime, Get, 2);
            await actor.InvokeAsync(async c => await put(c, [exported, Term.I(1), Term.I(StoredValue)]));
            Term read = await actor.InvokeAsync(async c => await get(c, [exported, Term.I(1)]));
            Require(read.Equals(Term.I(StoredValue)));
            Require((bool)Property(fixture.Array, AtomicsP2a586Names.Allocated));
            Require(!((Task)Property(fixture.Array, AtomicsP2a586Names.Retired)).IsCompleted);
            Require(carrier is not null);
            carrier!.Dispose();
            carrier = null;
            await ((Task)Property(fixture.Array, AtomicsP2a586Names.Retired)).WaitAsync(AtomicsMagic586Names.Deadline);
            Require(!(bool)Property(fixture.Array, AtomicsP2a586Names.Allocated));
            GC.KeepAlive(exported);
            GC.KeepAlive(fixture.Reference);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            try
            {
                carrier?.Dispose();
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
            if (fixture is not null)
                await fixture.Drain(failures);
            GC.KeepAlive(exported);
        }
        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(CleanupFailure, failures);
    }

    private static ErlangFunction Native(ProcessRuntime runtime, string function, int arity)
    {
        MethodInfo capture = typeof(ProcessRuntime).Assembly.GetType(NativeType, true)!.GetMethod(Capture, Members)!;
        object?[] arguments = [runtime, GuestModule, function, arity, null];
        Require((bool)capture.Invoke(null, arguments)!);

        return (ErlangFunction)arguments[4]!;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
