using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.AtomicsReservation591Names;

namespace Erlang.Tests;

internal static class AtomicsReservation591Checks
{
    internal static async Task PublicValueAndClose()
    {
        foreach (bool halt in new[] { false, true })
            foreach (bool compound in new[] { false, true })
                await Run(halt, compound, false);
    }

    internal static Task ActivePinStillJoins() => Run(false, true, true);

    internal static async Task RetainedReservationCloseOnly()
    {
        foreach (bool halt in new[] { false, true }) await Run(
            halt,
            true,
            false,
            false
        );
    }

    private static Task Run(
        bool halt,
        bool compound,
        bool activePin,
        bool validateValue = true
    ) => AtomicsP2a586Checks.Run(async f =>
    {
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? actor = null;
        IDisposable? frame = null, producer = null, pin = null;
        RuntimeExternalTermReservation? reservation = null;
        Task? shutdown = null;
        try
        {
            producer = f.Lease(AtomicsP2a586Names.ProducerKind);
            actor = new(f.Runtime);
            ProcessContext context = await actor.Ready.WaitAsync(AtomicsMagic586Names.Deadline);
            object heap = Property(context, AtomicsMagic586Names.Heap);
            frame = (IDisposable)Call(heap, AtomicsMagic586Names.Enter)!;
            int slot = (int)Call(frame, AtomicsMagic586Names.Reserve)!;
            Call(
                frame,
                AtomicsMagic586Names.Set,
                slot,
                f.Reference
            );
            var source = (ReferenceTerm)Call(frame, AtomicsMagic586Names.Read, slot)!;
            Term graph = compound ? Term.Tuple(Term.A(ExternalLabel), Term.List(source), new MapTerm(new[] { new KeyValuePair<Term, Term>(Term.A(ReferenceKey), source) })) : source;
            reservation = f.Runtime.ReserveExternalTerm(graph);
            f.ReleaseProducer();
            producer.Dispose();
            producer = null;
            Call(frame, AtomicsMagic586Names.Clear, slot);
            Call(heap, AtomicsMagic586Names.Collect);
            Term retained = reservation.Value;
            var reference = compound ? (ReferenceTerm)((Cons)((TupleTerm)retained).Items[1]).Head : (ReferenceTerm)retained;
            if (validateValue)
            {
                ErlangFunction put = Native(f.Runtime, Put, 3), get = Native(f.Runtime, Get, 2);
                await actor.InvokeAsync(async c => await put(c, [reference, Term.I(1), Term.I(StoredValue)]));
                Require((await actor.InvokeAsync(async c => await get(c, [reference, Term.I(1)]))).Equals(Term.I(StoredValue)));
                if (compound)
                {
                    var map = (MapTerm)((TupleTerm)retained).Items[2];
                    var second = (ReferenceTerm)map.Entries.Single().Value;
                    Require((await actor.InvokeAsync(async c => await get(c, [second, Term.I(1)]))).Equals(Term.I(StoredValue)));
                }
            }
            frame.Dispose();
            frame = null;
            if (activePin) pin = f.Pin();
            ReleaseFixtureInvocation(f);
            shutdown = halt ? Halt(f.Runtime) : f.Runtime.DisposeAsync().AsTask();
            if (activePin)
            {
                using var deadline = new CancellationTokenSource(AtomicsMagic586Names.Deadline);
                while (!ArrayClosing(f.Array))
                    await Task.Delay(PollIntervalMilliseconds, deadline.Token);
                Require(!shutdown.IsCompleted && !((Task)Property(f.Array, AtomicsP2a586Names.Retired)).IsCompleted);
                pin!.Dispose();
                pin = null;
            }
            await shutdown.WaitAsync(AtomicsMagic586Names.Deadline);
            Require(f.Runtime.AtomicsStorage413.LiveArrays == 0 && f.Runtime.AtomicsStorage413.OwnedNativeBytes == 0);
            // Reservation remains held until finally; it must not be a hidden
            // untracked Guest root that makes whole-VM shutdown wait forever.
            GC.KeepAlive(reservation);
            GC.KeepAlive(retained);
            GC.KeepAlive(source);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            try
            {
                reservation?.Dispose();
            }
            catch (Exception e)
            {
                failures.Add(e);
            }
            try
            {
                pin?.Dispose();
            }
            catch (Exception e)
            {
                failures.Add(e);
            }
            try
            {
                frame?.Dispose();
            }
            catch (Exception e)
            {
                failures.Add(e);
            }
            try
            {
                producer?.Dispose();
            }
            catch (Exception e)
            {
                failures.Add(e);
            }
            try
            {
                ReleaseFixtureInvocation(f);
            }
            catch (Exception e)
            {
                failures.Add(e);
            }
            if (shutdown is not null) try
                {
                    await shutdown.WaitAsync(AtomicsMagic586Names.Deadline);
                }
                catch (Exception e)
                {
                    failures.Add(e);
                }
            if (actor is not null) try
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(AtomicsMagic586Names.Deadline);
                }
                catch (Exception e)
                {
                    failures.Add(e);
                }
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(failures);
    });

    private static bool ArrayClosing(object array)
    {
        object gate = array.GetType().GetField(GateField, Members)!.GetValue(array)!;
        lock (gate) return (bool)array.GetType().GetField(ClosedField, Members)!.GetValue(array)!;
    }

    private static async Task Halt(ProcessRuntime runtime)
    {
        await runtime.RequestHalt(0);
        await runtime.HaltQuiescence;
    }

    private static void ReleaseFixtureInvocation(AtomicsP2a586Fixture fixture)
    {
        FieldInfo field = typeof(AtomicsP2a586Fixture).GetField(InvocationField, Members)!;
        var invocation = (IDisposable?)field.GetValue(fixture);
        invocation?.Dispose();
        field.SetValue(fixture, null);
    }

    private static ErlangFunction Native(ProcessRuntime runtime, string function, int arity)
    {
        object?[] arguments = [runtime, GuestModule, function, arity, null];
        Require((bool)typeof(ProcessRuntime).Assembly.GetType(NativeType, true)!.GetMethod(Capture, Members)!.Invoke(null, arguments)!);

        return (ErlangFunction)arguments[4]!;
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }

}
