using Erlang;
using Erlang.Differential;

namespace Erlang.Tests;

internal static class DistributionGuestSendPublicRouteHostChecks
{
    internal static async Task Run()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        using var compiled = GeneratedModuleCompiler.Compile(DistributionGuestSendHostNames.Source);
        compiled.Definition.Register(fixture.Runtime.Modules);
        await fixture.Producer.InvokeAsync(async context =>
        {
            Term leader = await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                DistributionGuestSendHostNames.GroupLeader,
                context.Self,
                fixture.Connection.Remote
            );
            Check(leader.Equals(Term.A(ErlangBooleanAtoms.True)));
            Term first = Term.A(DistributionGuestSendHostNames.First);
            Check((await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                DistributionGuestSendHostNames.Send,
                fixture.Connection.Remote,
                first
            )).Equals(first));
            Term second = Term.A(DistributionGuestSendHostNames.Second);
            Check((await fixture.Runtime.Modules.Call(
                context,
                DistributionGuestSendHostNames.GeneratedModule,
                DistributionGuestSendHostNames.Run,
                fixture.Connection.Remote,
                second
            )).Equals(second));
            Term third = Term.A(DistributionGuestSendHostNames.Third);
            Check((await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                DistributionGuestSendHostNames.Bang,
                fixture.Connection.Remote,
                third
            )).Equals(third));

            return true;
        });
        var control = await Take();
        Check(control.Payload is null && control.Control.Equals(Term.Tuple(
            Term.I(DistributionControlOpcodes.GroupLeader), fixture.Producer.Process.Pid, fixture.Connection.Remote)));
        foreach (string marker in new[] { DistributionGuestSendHostNames.First, DistributionGuestSendHostNames.Second, DistributionGuestSendHostNames.Third })
        {
            var packet = await Take();
            Check(packet.Control.Equals(Term.Tuple(Term.I(DistributionControlOpcodes.Send),
                Term.A(DistributionPassThroughLayout.EmptyCookie), fixture.Connection.Remote)));
            Check(packet.Payload is not null && packet.Payload.Equals(Term.A(marker)));
        }
        Check(fixture.Runtime.DistributionPending.ConnectedSnapshot(fixture.Connection.Host).QueuedPackets == 0);

        async Task<DistributionDecodedProcessPacket> Take()
        {
            var buffer = await fixture.Connection.Controller.Invoke(context =>
                fixture.Runtime.DistributionPending.TakeConnectedOutput(fixture.Connection.Host, context, out _));
            // Public operations have already returned; this checks actual publication, not a helper-shaped expectation.
            Check(buffer is not null);
            Check(buffer!.SegmentCount == 1);

            return DistributionPassThroughCodec.DecodeProcessPacket(buffer.Segments[0].ToArray());
        }
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DistributionGuestSendHostNames.Failure);
    }
}
