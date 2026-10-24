using static Erlang.Tests.BeamGuardedTail398Names;
using Erlang;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamBinaryHostBytes;

namespace Erlang.Tests;

internal static class BeamGuardedTail398Checks
{

    private static byte[] Image(int unit, bool equalityAfterWrite = false, int ensureUnit = 1)
    {
        byte[] commands = Join(
            A(BeamBinaryHostNames.EnsureAtom),
            U(8),
            U(ensureUnit),
            A(BeamBinaryHostNames.IntegerAtom),
            U(2),
            A(0),
            U(8),
            U(1),
            X(0),
            equalityAfterWrite
                ? Join(
                    A(BeamBinaryHostNames.EqualAtom),
                    A(0),
                    U(8),
                    U(0)
                )
                : Join(
                    A(BeamBinaryHostNames.GetTailAtom),
                    U(2),
                    U(unit),
                    X(1)
                )
        );

        return BeamBinaryHostBytes.Image(Join(
            Start(1, 0, 1),
            Instruction(
                SavePosition,
                X(1),
                X(0),
                U(2)
            ),
            Instruction(
                BeamBinaryHostNames.MatchOpcode,
                F(3),
                X(1),
                Join([0x17], U(13), commands)
            ),
            equalityAfterWrite ? Instruction(Move, A(7), X(0)) : [],
            [Return],
            Instruction(Label, U(3)),
            Instruction(RestorePosition, X(1), X(0)),
            Instruction(
                ExtractTail,
                X(1),
                X(0),
                U(2)
            ),
            [Return]
        ));
    }

    internal static async Task AlignedTailAfterPositionOverwrite()
    {
        foreach (int unit in new[] { 2, 8 })
        {
            BeamModule prepared = BeamModule.Prepare(Image(unit, ensureUnit: unit));
            Require(prepared.Supports(BeamOpcodeEdgeNames.Run, 1));
            await using var runtime = new ProcessRuntime();
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            Require(await actor.InvokeAsync(context => prepared.Invoke(context, BeamOpcodeEdgeNames.Run, new BitString([0x81])).AsTask())
                is Integer { Value: var value } && value == 0x81);
            BitString empty = new([]);
            Require((await actor.InvokeAsync(context => prepared.Invoke(context, BeamOpcodeEdgeNames.Run, empty).AsTask())).Equals(empty));
        }
    }

    internal static Task RealFailuresStillRefuse()
    {
        Reject(Image(2));
        Reject(Image(1, equalityAfterWrite: true));
        Reject(BeamBinaryHostBytes.Image(Join(Start(1, 0, 0), [Return])));

        return Task.CompletedTask;
    }

    private static void Reject(byte[] image)
    {
        bool refused;
        try
        {
            refused = !BeamModule.Prepare(image).Supports(BeamOpcodeEdgeNames.Run, 1);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            refused = true;
        }
        Require(refused);
    }
}
