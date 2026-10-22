using static Erlang.Tests.BeamPositionOwner398Names;
using Erlang;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamBinaryHostBytes;

namespace Erlang.Tests;

internal static class BeamPositionOwner398Checks
{

    internal static async Task SourceIdentityStillRequired()
    {
        byte[] image = BeamBinaryHostBytes.Image(
            Join(
                Start(2, 0, 0),
                Start(2, 1, 1),
                Instruction(
                    SavePosition,
                    X(0),
                    X(2),
                    U(2)
                ),
                Instruction(RestorePosition, X(1), X(2)),
                Instruction(
                    ExtractTail,
                    X(1),
                    X(0),
                    U(2)
                ),
                [Return]
            ),
            arity: 2
        );
        BeamModule prepared = BeamModule.Prepare(image);
        Require(prepared.Supports(BeamOpcodeEdgeNames.Run, 2));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        BitString first = new([0xab]);
        BitString second = new([0xab]);
        bool refused = false;
        try
        {
            await actor.InvokeAsync(context => prepared.Invoke(
                context,
                BeamOpcodeEdgeNames.Run,
                first,
                second
            ).AsTask());
        }
        catch (InvalidDataException)
        {
            refused = true;
        }
        Require(refused);
        Require((await actor.InvokeAsync(context => prepared.Invoke(
            context,
            BeamOpcodeEdgeNames.Run,
            first,
            first
        ).AsTask())).Equals(first));
    }
}
