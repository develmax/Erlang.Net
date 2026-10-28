using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.DebugDirtyAtomics595Names;
using static Erlang.Tests.NativeResource524Access;

namespace Erlang.Tests;

// Direct typed-copy controls only. The unchanged genuine compiled424 worker
// remains the proof of actual dirty scheduling and its complete original law.
internal static class DebugDirtyAtomics595Checks
{
    internal static Task CopiedWrapperSurvivesSourceDrop() => Run(false);

    internal static Task ForeignBeforeStale() => Run(true);

    private static Task Run(bool foreignProbe) => AtomicsP2a586Checks.Run(async fixture =>
    {
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? actor = null;
        ProcessRuntime? foreign = null;
        IDisposable? frame = null, producer = null, carrier = null;
        Term? source = null, copy = null;
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
            Term input = source;
            copy = await actor.Invoke(_ => DirtyCopy(fixture.Runtime, input));
            carrier = typeof(ResourceReferenceTerm).GetProperty(HostCarrier, Members)!.GetValue(copy) as IDisposable;
            Require(!ReferenceEquals(copy, source) && copy.Equals(fixture.Reference));
            fixture.ReleaseProducer();
            producer.Dispose();
            producer = null;
            Call(frame, AtomicsMagic586Names.Clear, slot);
            Call(heap, AtomicsMagic586Names.Collect);
            ErlangFunction put = Native(fixture.Runtime, Put, 3), get = Native(fixture.Runtime, Get, 2);
            Term retained = copy;
            await actor.InvokeAsync(async c => await put(c, [retained, Term.I(1), Term.I(StoredValue)]));
            Require((await actor.InvokeAsync(async c => await get(c, [retained, Term.I(1)]))).Equals(Term.I(StoredValue)));
            Require(carrier is not null);
            if (foreignProbe)
            {
                foreign = OwnedGuest506RuntimeFactory.Create();
                Expect<NotSupportedException>(() => DirtyCopy(foreign!, input));
                Expect<ObjectDisposedException>(() => DirtyCopy(fixture.Runtime, input));
            }
            carrier!.Dispose();
            carrier = null;
            await ((Task)Property(fixture.Array, AtomicsP2a586Names.Retired)).WaitAsync(AtomicsMagic586Names.Deadline);
            Require(!(bool)Property(fixture.Array, AtomicsP2a586Names.Allocated));
            GC.KeepAlive(source);
            GC.KeepAlive(copy);
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
            if (foreign is not null)
                try
                {
                    await foreign.DisposeAsync().AsTask().WaitAsync(AtomicsMagic586Names.Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            GC.KeepAlive(source);
            GC.KeepAlive(copy);
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(CleanupFailure, failures);
    });

    private static Term DirtyCopy(ProcessRuntime runtime, Term value)
    {
        try
        {
            return (Term)typeof(ProcessRuntime).Assembly.GetType(CopyType, true)!.GetMethod(Copy, Members)!.Invoke(null, [runtime, value])!;
        }
        catch (TargetInvocationException failure) when (failure.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static ErlangFunction Native(ProcessRuntime runtime, string function, int arity)
    {
        object?[] arguments = [runtime, GuestModule, function, arity, null];
        Require((bool)typeof(ProcessRuntime).Assembly.GetType(NativeType, true)!.GetMethod(Capture, Members)!.Invoke(null, arguments)!);

        return (ErlangFunction)arguments[4]!;
    }

    private static void Expect<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException(Failure);
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
