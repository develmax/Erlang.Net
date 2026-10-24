using Erlang;

namespace Erlang.Tests;

internal static class BeamClosureProtocolHostChecks
{
    internal static Task AllRaise() => Accept(BeamClosureProtocolShape.AllRaise);

    internal static Task OneReturns() => Refuse(BeamClosureProtocolShape.OneReturns);

    internal static Task DefaultReturns() => Refuse(BeamClosureProtocolShape.DefaultReturns);

    internal static Task EntryCycle() => Refuse(BeamClosureProtocolShape.EntryCycle);

    internal static Task LocalCycle() => Refuse(BeamClosureProtocolShape.LocalCycle);

    internal static Task CaughtReturn() => Refuse(BeamClosureProtocolShape.CaughtReturn);

    internal static Task SharedDag() => Accept(BeamClosureProtocolShape.SharedDag);

    internal static async Task YCommit()
    {
        BeamModule module = BeamModule.Prepare(BeamClosureProtocolReceiveBytes.Image());
        Require(module.Supports(BeamOpcodeEdgeNames.Run, 0));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term rejected = Term.A(BeamOpcodeEdgeNames.Marker);
            Term selected = Term.A(BeamOpcodeEdgeNames.Other);
            context.Mailbox.Send(rejected);
            context.Mailbox.Send(selected);
            Term value = await module.Invoke(context, BeamOpcodeEdgeNames.Run);
            Require(value.Equals(selected) && context.Mailbox.Count == 1);
            Require((await context.ReceiveAsync(message => message, TimeSpan.Zero))!.Equals(rejected));

            return true;
        }).WaitAsync(BeamReceiveMarkerTypeHostNames.Deadline);
    }

    internal static Task YReject()
    {
        Reject(BeamClosureProtocolReceiveBytes.Image(rejectAfterWrite: true), 0);

        return Task.CompletedTask;
    }

    internal static Task HandlerOverwrite()
    {
        Reject(BeamClosureProtocolReceiveBytes.Image(overwriteHandler: true), 0);

        return Task.CompletedTask;
    }

    private static Task Accept(BeamClosureProtocolShape shape)
    {
        Require(BeamModule.Prepare(BeamClosureProtocolNoReturnBytes.Image(shape)).Supports(BeamOpcodeEdgeNames.Run, 1));

        return Task.CompletedTask;
    }

    private static Task Refuse(BeamClosureProtocolShape shape)
    {
        Reject(BeamClosureProtocolNoReturnBytes.Image(shape), 1);

        return Task.CompletedTask;
    }

    private static void Reject(byte[] bytes, int arity)
    {
        bool refused;
        try
        {
            refused = !BeamModule.Prepare(bytes).Supports(BeamOpcodeEdgeNames.Run, arity);
        }
        catch (Exception failure) when (failure is InvalidDataException or NotSupportedException)
        {
            refused = true;
        }
        Require(refused);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(BeamClosureProtocolNames.Failure);
    }
}
