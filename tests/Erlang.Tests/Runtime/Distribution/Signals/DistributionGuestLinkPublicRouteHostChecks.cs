using Erlang;

namespace Erlang.Tests;

internal static class DistributionGuestLinkPublicRouteHostChecks
{
    internal static async Task LinkPublishes()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        await TrapExits(fixture);
        Check((await Call(fixture, ErlangModuleNames.Link)).Equals(Term.A(ErlangBooleanAtoms.True)));
        // The operation returned; require actual output now, rather than timing out on an absent registration.
        Check((await TakeNow(fixture)).Control.Equals(LinkControl(fixture)));
        Check((await Links(fixture)).Equals(Term.List(fixture.Connection.Remote)));
        Check((await SelfLinks(fixture)).Equals(Term.List(fixture.Connection.Remote)));
        Check(await Receive(fixture) is null);
    }

    internal static async Task InboundUnlinkPublishes()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        await TrapExits(fixture);
        await Put(fixture, Term.Tuple(Term.I(DistributionControlOpcodes.Link), fixture.Connection.Remote, fixture.Producer.Process.Pid));
        Check(await Receive(fixture) is null);
        Check((await Links(fixture)).Equals(Term.List(fixture.Connection.Remote)));
        Check((await SelfLinks(fixture)).Equals(Term.List(fixture.Connection.Remote)));
        Check((await Call(fixture, ErlangModuleNames.Unlink)).Equals(Term.A(ErlangBooleanAtoms.True)));
        var control = (await TakeNow(fixture)).Control;
        Check(control.Items.Count == 4 && control.Items[0].Equals(Term.I(DistributionControlOpcodes.UnlinkId)) &&
            control.Items[DistributionGuestLinkHostNames.UnlinkIdIndex] is Integer { Value.Sign: > 0 } &&
            control.Items[2].Equals(fixture.Producer.Process.Pid) && control.Items[3].Equals(fixture.Connection.Remote));
        Check((await Links(fixture)).Equals(Nil.Value));
        Check((await SelfLinks(fixture)).Equals(Nil.Value));
    }

    internal static Task<Term> Call(DistributionLifecycleHostFixture fixture, string function, Term? options = null)
        => fixture.Producer.InvokeAsync(async context => options is null
            ? await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                function,
                fixture.Connection.Remote
            )
            : await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                function,
                fixture.Connection.Remote,
                options
            ));

    internal static Task<bool> TrapExits(DistributionLifecycleHostFixture fixture)
        => fixture.Producer.Invoke(context =>
        {
            context.TrapExits = true;

            return true;
        });

    internal static Task<Term?> Receive(DistributionLifecycleHostFixture fixture)
        => fixture.Producer.InvokeAsync(async context => await context.ReceiveAsync<Term>(message => message, TimeSpan.Zero));

    internal static async Task<Term> Links(DistributionLifecycleHostFixture fixture)
    {
        // HOST state observation, including phases where the producer is blocked in an output operation.
        ProcessContext context = await fixture.Producer.Ready;
        object gate = typeof(ProcessRuntime).GetField(
            DistributionGuestLinkHostNames.RuntimeGateMember, DistributionGuestLinkHostNames.Members)!.GetValue(fixture.Runtime)!;
        lock (gate)
        {
            var links = (HashSet<Pid>)typeof(ProcessContext).GetProperty(
                DistributionGuestLinkHostNames.LinksMember, DistributionGuestLinkHostNames.Members)!.GetValue(context)!;

            return Cons.From(links.Cast<Term>().ToArray());
        }
    }

    private static Task<Term> SelfLinks(DistributionLifecycleHostFixture fixture)
        => fixture.Producer.InvokeAsync(async context =>
        {
            var information = (TupleTerm)await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ProcessInfoNames.Function,
                context.Self,
                Term.A(ProcessInfoNames.Links)
            );

            return information.Items[1];
        });

    internal static async Task Put(DistributionLifecycleHostFixture fixture, TupleTerm control)
    {
        byte[] packet = DistributionPassThroughCodec.EncodeProcessControl(control);
        Term result = await fixture.Connection.Controller.Invoke(context => DistributionInputAdapter.PutData(
            context, fixture.Connection.Opaque, new BitString(packet)));
        Check(result.Equals(Term.A(ConnectedDistributionHostNames.Success)));
    }

    internal static async Task<DistributionDecodedProcessPacket> TakeNow(DistributionLifecycleHostFixture fixture)
    {
        var buffer = await fixture.Connection.Controller.Invoke(context => fixture.Runtime.DistributionPending.TakeConnectedOutput(
            fixture.Connection.Host, context, out _));
        Check(buffer is { SegmentCount: 1 });

        return DistributionPassThroughCodec.DecodeProcessPacket(buffer!.Segments[0].ToArray());
    }

    internal static TupleTerm LinkControl(DistributionLifecycleHostFixture fixture)
        => Term.Tuple(Term.I(DistributionControlOpcodes.Link), fixture.Producer.Process.Pid, fixture.Connection.Remote);

    internal static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DistributionGuestLinkHostNames.Failure);
    }
}
