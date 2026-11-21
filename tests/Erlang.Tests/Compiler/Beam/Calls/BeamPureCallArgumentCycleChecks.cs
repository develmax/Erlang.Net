using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamPureCallArgumentCycleChecks
{
    internal const string TestName = "compiler/beam/folds-closed-call-argument-cycles";
    private const string WrongCycle = "A closed call argument cycle changed its register order or reused a live scratch.";
    private const int Entry = 2;
    private const int Labels = 3;
    private const int Arity = 4;

    internal static Task Run()
    {
        byte[] moves =
        [
            Move, X(1), X(4),
            Move, X(0), X(1),
            Move, X(2), X(0),
            Move, X(3), X(2),
            Move, X(4), X(3)
        ];
        byte[] expected =
        [
            Swap, X(3), X(2),
            Swap, X(1), X(3),
            Swap, X(0), X(1)
        ];
        byte[] body = Image(moves, Arity);
        if (Rewrite(body) is not { } rewritten || !rewritten.SequenceEqual(Image(expected, Arity))
            || Rewrite(Image(moves, Arity + 1)) is not null)
        {
            throw new InvalidOperationException(WrongCycle);
        }

        return Task.CompletedTask;
    }

    private static byte[] Image(byte[] moves, int arity) =>
    [
        .. Start(
            1,
            Entry,
            2,
            Arity
        ),
        .. moves,
        TailLocalCall, U(arity), F(Entry), EndCode
    ];

    private static byte[]? Rewrite(ReadOnlyMemory<byte> code)
    {
        object header = New(
            "BeamCodeHeader",
            0,
            (int)DebugLine,
            Labels,
            1,
            code
        );
        object instructions = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            8,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewritePureCallArgumentCycles",
            code,
            instructions
        );
    }
}
