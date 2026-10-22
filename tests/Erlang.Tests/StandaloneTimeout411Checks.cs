using Erlang;
using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamReceiveMarkerTypeHostNames;

namespace Erlang.Tests;

internal static class StandaloneTimeout411Checks
{
    private static BeamModule Prepare(byte[] body)
        => BeamModule.Prepare(BeamReceiveMarkerTypeBytes.Image(body, arity: 1, labels: 8));

    private static void RequireTypedRefusal(byte[] body)
    {
        try
        {
            BeamBinaryHostAccess.Require(!Prepare(body).Supports(BeamOpcodeEdgeNames.Run, 1));
        }
        catch (InvalidDataException failure)
        {
            BeamBinaryHostAccess.Require(failure.Message == StandaloneTimeout411Names.FrameError);
        }
    }

    internal static async Task Positive()
    {
        byte[] ordinary = [Allocate, U(1), U(1), Move, X(0), Y(0), TimeoutOpcode,
            Move, Y(0), X(0), Deallocate, U(1), Return];
        byte[] protectedBody = [Allocate, U(2), U(1), Move, X(0), Y(0), Try, Y(1), F(7),
            TimeoutOpcode, TryEnd, Y(1), Move, Y(0), X(0), Deallocate, U(2), Return,
            Label, U(7), TryCase, Y(1), Move, Y(0), X(0), Deallocate, U(2), Return];
        await using var runtime = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        foreach (byte[] body in new[] { ordinary, protectedBody })
        {
            BeamModule module = Prepare(body);
            BeamBinaryHostAccess.Require(module.Supports(BeamOpcodeEdgeNames.Run, 1));
            Term argument = Term.A(BeamOpcodeEdgeNames.Marker);
            Term result = await actor.InvokeAsync(context => module.Invoke(
                context, BeamOpcodeEdgeNames.Run, argument
            ).AsTask());
            BeamBinaryHostAccess.Require(result.Equals(argument));
        }
    }

    internal static Task Refusal()
    {
        RequireTypedRefusal([TimeoutOpcode, Move, X(0), X(0), Return]);
        RequireTypedRefusal([Allocate, U(1), U(1), TimeoutOpcode,
            Move, A(7), X(0), Deallocate, U(1), Return]);
        byte[] opaque = BeamBinaryHostBytes.Join(BeamBinaryHostBytes.Start(1, 0, 1),
            [TimeoutOpcode, Move, A(7), X(0), Return]);
        BeamBinaryHostAccess.Require(!BeamModule.Prepare(BeamBinaryHostBytes.Image(opaque, arity: 1))
            .Supports(BeamOpcodeEdgeNames.Run, 1));

        return Task.CompletedTask;
    }
}
