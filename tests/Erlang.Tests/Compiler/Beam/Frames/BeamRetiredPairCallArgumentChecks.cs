using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamRetiredPairCallArgumentChecks
{
    internal const string TestName = "compiler/beam/reuses-a-retired-pair-call-argument-slot";
    private const string WrongLayout = "Pair construction should hold the earlier result across its heap check and reuse the consumed slot.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_retired_pair_call_argument");
        if (Apply(Fixture()) is not { } changed || !changed.SequenceEqual(Fixture(reused: true))
            || Apply(changed) is not null || Apply(Fixture(readExpired: true)) is not null
            || Apply(Fixture(touchedSpill: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        byte[]? Apply(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "ReuseRetiredPairCallArgument",
            code,
            4
        );
    }

    private static byte[] Fixture(bool reused = false, bool readExpired = false, bool touchedSpill = false)
    {
        byte field = Y(reused ? 0 : 1);
        byte expired = Y(reused ? 1 : 2);
        int frame = reused ? 2 : 3;
        byte[] initializing = reused ? [] : [InitYRegisters, ExtendedListOperand, U(1), Y(0)];
        byte[] returnSlot = touchedSpill ? [Move, X(0), Y(0)] : [];
        byte[] setup = reused
            ?
            [
                TestHeap, U(3), U(1), PutTuple, X(1), ExtendedListOperand, U(2), field, I(0),
                Move, expired, X(2), Move, X(0), expired, Move, X(1), X(0), Move, X(2), X(1),
                InitYRegisters, ExtendedListOperand, U(1), field
            ]
            :
            [
                Move, X(0), Y(0), TestHeap, U(3), U(0), PutTuple, X(0), ExtendedListOperand, U(2), field, I(0),
                Move, expired, X(1), InitYRegisters, ExtendedListOperand, U(1), expired
            ];
        byte[] control = readExpired ? [Move, expired, X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(frame), U(2), .. initializing,
            Move, X(0), field, Move, X(1), expired, .. returnSlot,
            Line, U(1), LocalCall, U(2), F(2),
            .. setup, Line, U(1), LocalCall, U(2), F(2),
            ExactEqualTest, F(3), X(0), A(1),
            Move, Y(reused ? 1 : 0), X(0), Deallocate, U(frame), Return,
            Label, U(3), .. control, Deallocate, U(frame), Return,
            EndCode
        ];
    }
}
