using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.CountersNative596Names;
using static Erlang.Tests.NativeResource524Access;

namespace Erlang.Tests;

internal static class CountersNative596Checks
{
    internal static Task RealNativeFamily() => CountersCustody596Checks.Run(async fixture =>
    {
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? actor = null;
        IDisposable? host = null;
        object? actualArray = null;
        try
        {
            actor = new(fixture.Runtime);
            ErlangFunction create = Native(fixture.Runtime, New, 1), get = Native(fixture.Runtime, Get, 2),
                add = Native(fixture.Runtime, Add, 3), put = Native(fixture.Runtime, Put, 3), info = Native(fixture.Runtime, Info, 1);
            var reference = (ResourceReferenceTerm)await actor.InvokeAsync(async context => await create(context, [Term.I(ArrayLength)]));
            host = Property(reference, Host) as IDisposable ?? throw new InvalidOperationException(Failure);
            actualArray = Property(Property(reference, Backing), BackingArray);
            Require((await actor.InvokeAsync(async context => await get(context, [reference, Term.I(LastIndex)]))).Equals(Term.I(0)));
            await actor.InvokeAsync(async context => await put(context, [reference, Term.I(LastIndex), Term.I(Initial)]));
            await actor.InvokeAsync(async context => await add(context, [reference, Term.I(LastIndex), Term.I(1)]));
            Require((await actor.InvokeAsync(async context => await get(context, [reference, Term.I(LastIndex)]))).Equals(Term.I(Wrapped)));
            var summary = (MapTerm)await actor.InvokeAsync(async context => await info(context, [reference]));
            using (var pin = (IDisposable)Call(actualArray, Acquire)!)
            {
                Require(summary.TryGet(Term.A(Size), out var size) && size!.Equals(Term.I(ArrayLength)));
                Require(summary.TryGet(Term.A(Memory), out var bytes) && bytes!.Equals(new Integer((ulong)Property(actualArray, CountersCustody596Names.OriginalSize))));
            }
            // A canonical registry entry and live backing are insufficient without a real wrapper carrier/header.
            using (fixture.Lease(CountersCustody596Names.ProducerKind))
                await Bad(() => actor.InvokeAsync(async context => await get(context, [fixture.Reference, Term.I(1)])));
            await Bad(() => actor.InvokeAsync(async context => await get(context, [Term.I(0), Term.I(1)])));
            await Bad(() => actor.InvokeAsync(async context => await add(context, [reference, Term.I(0), Term.I(1)])));
            await Bad(() => actor.InvokeAsync(async context => await put(context, [reference, Term.I(LastIndex), new Integer(System.Numerics.BigInteger.One << WordBits)])));
            await Bad(() => actor.InvokeAsync(async context => await create(context, [Term.I(0)])));
            host.Dispose();
            host = null;
            await ((Task)Property(actualArray, CountersCustody596Names.Retired)).WaitAsync(CountersMagic596Names.Deadline);
            Require(!(bool)Property(actualArray, CountersCustody596Names.Allocated));
            GC.KeepAlive(reference);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        finally
        {
            try
            {
                host?.Dispose();
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(CountersMagic596Names.Deadline);
                }
                catch (Exception failure)
                {
                    failures.Add(failure);
                }
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(CleanupFailure, failures);
    });

    private static ErlangFunction Native(ProcessRuntime runtime, string function, int arity)
    {
        object?[] arguments = [runtime, GuestModule, function, arity, null];
        Require((bool)typeof(ProcessRuntime).Assembly.GetType(NativeType, true)!.GetMethod(Capture, Members)!.Invoke(null, arguments)!);

        return (ErlangFunction)arguments[4]!;
    }

    private static async Task Bad(Func<Task<Term>> action)
    {
        try
        {
            _ = await action();
        }
        catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
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
