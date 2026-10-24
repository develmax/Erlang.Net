using Erlang;

namespace Erlang.Tests;

internal static class DistributionGuestExitPublicRouteHostChecks
{
    internal static Task ExitTwoPublishes() => Run(false);

    internal static Task ExitThreePublishes() => Run(true);

    private static async Task Run(bool options)
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        Term reason = Term.A(DistributionGuestExitHostNames.Reason);
        Term result = await fixture.Producer.InvokeAsync(async context => await Call(
            context,
            ErlangModuleNames.Exit,
            fixture.Connection.Remote,
            reason,
            options ? Term.List(Term.A(DistributionGuestExitHostNames.Priority)) : null
        ));
        Check(result.Equals(Term.A(ErlangBooleanAtoms.True)));
        var buffer = await fixture.Connection.Controller.Invoke(context => fixture.Runtime.DistributionPending.TakeConnectedOutput(
            fixture.Connection.Host, context, out _));
        // Assert actual output immediately after the public operation returned; historical code returns true with no packet.
        Check(buffer is { SegmentCount: 1 });
        var packet = DistributionPassThroughCodec.DecodeProcessPacket(buffer!.Segments[0].ToArray());
        Check(packet.Payload is null && packet.Control.Equals(Term.Tuple(
            Term.I(DistributionControlOpcodes.Exit2),
            fixture.Producer.Process.Pid,
            fixture.Connection.Remote,
            reason
        )));
    }

    internal static ValueTask<Term> Call(
        ProcessContext context,
        string function,
        Term target,
        Term reason,
        Term? options = null
    )
        => options is null
            ? context.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                function,
                target,
                reason
            )
            : context.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                function,
                target,
                reason,
                options
            );

    internal static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DistributionGuestExitHostNames.Failure);
    }
}
