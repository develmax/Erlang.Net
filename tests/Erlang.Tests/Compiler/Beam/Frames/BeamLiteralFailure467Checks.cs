using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamFrameJoin394Names;
using static Erlang.Tests.LibrarySelectionHostData;

namespace Erlang.Tests;

internal static class BeamLiteralFailure467Checks
{
    internal static Task Admission()
    {
        Require(Admitted(Image()));
        Require(!Admitted(Image(returning: true)));
        Require(!Admitted(Image(unknown: true)));
        Require(!Admitted(Image(branch: true)));
        Require(!Admitted(Image(loop: true)));
        Require(!Admitted(Image(invalidY: true)));

        return Task.CompletedTask;
    }

    private static byte[] Image(
        bool returning = false,
        bool unknown = false,
        bool branch = false,
        bool loop = false,
        bool invalidY = false
    ) => BeamOpcodeEdgeBytes.Image(
        [
        .. Start(
            1,
            2,
            2,
            1
        ), Allocate, U(2), U(1), Move, X(0), Y(invalidY ? 2 : 0),
        BeamLiteralFailure467Names.Try, Y(1), F(3),
        .. (branch ? new byte[] { Jump, F(6) } : []),
        .. (unknown ? [] : new byte[] { Move, A(returning ? 0 : 7), X(0) }),
        .. (branch ? new byte[] { Label, U(6) } : []),
        BeamLiteralFailure467Names.Call, U(1), F(5),
        Label, U(3), BeamLiteralFailure467Names.TryCase, Y(1), Deallocate, U(2), Return,
        .. Start(
            4,
            5,
            3,
            1
        ),
        .. (loop ? new byte[] { Jump, F(5) } : new byte[] { BeamLiteralFailure467Names.NilTest, F(4), X(0), Return })
    ],
        labels: 7,
        functions: 2
    );

    private static bool Admitted(byte[] image)
    {
        try
        {
            return BeamModule.Prepare(image).Supports(BeamOpcodeEdgeNames.Run, 1);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return false;
        }
    }
}
