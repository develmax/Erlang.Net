using static Erlang.Tests.DebugGuestIdentity377Names;
using static Erlang.Tests.DebugGuestIdentity377Checks;

namespace Erlang.Tests;

internal static class DebugGuestCounter377Checks
{
    internal static Task Run() => CountersCustody596Checks.Run(async fixture =>
    {
        using IDisposable producer = fixture.Lease(CountersCustody596Names.ProducerKind);
        await DebugSharing425Access.PrepareSourceRuntime(fixture.Runtime);
        await Prepare(fixture.Runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(fixture.Runtime);
        await actor.InvokeAsync(async context =>
        {
            Term yes = Term.A(ErlangBooleanAtoms.True);
            Term outcome = await fixture.Runtime.Modules.Call(
                context,
                GuestModule,
                CounterMethod,
                fixture.Reference
            );
            Require(outcome.Equals(Term.Tuple(yes, yes, yes)));
            var actual = (TupleTerm)Read(context, CounterKey);
            Require(Property(actual.Items[0], FunctionStorage) is not null);
            Require(HostRoots(fixture.Array) == 0);
            fixture.ReleaseProducer();
            producer.Dispose();
            CollectMajor(context);
            Require((bool)Property(fixture.Array, CountersCustody596Names.Allocated));
            Require(!((Task)Property(fixture.Array, CountersCustody596Names.Retired)).IsCompleted);
            Require(context.Dictionary.Remove(Term.A(CounterKey)));
            CollectMajor(context);

            return Nil.Value;
        });
        await ((Task)Property(fixture.Array, CountersCustody596Names.Retired)).WaitAsync(CountersMagic596Names.Deadline);
        Require(HostRoots(fixture.Array) == 0 && !(bool)Property(fixture.Array, CountersCustody596Names.Allocated));
        GC.KeepAlive(fixture.Reference);
    });

    private static long HostRoots(object array)
    {
        lock (Field(array, CountersExportRollback596Names.ArrayGateField))
            return (long)Field(array, CountersExportRollback596Names.HostRootsField);
    }
}
