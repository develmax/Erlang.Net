using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamDisplacedBranchCallArgumentChecks
{
    internal const string TestName = "compiler/beam/reuses-expired-call-argument-across-branch-continuations";
    private const string IncorrectLayout = "The expired argument can share a slot only when every continuing path owns the new value.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_displaced_branch_call_argument");
        if (Rewrite(Fixture()) is not { } changed || !changed.SequenceEqual(Fixture(compacted: true))
            || Rewrite(changed) is not null || Rewrite(Fixture(retryReadsOld: true)) is not null
            || Rewrite(Fixture(bypassCall: true)) is not null)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        return Task.CompletedTask;

        byte[]? Rewrite(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "ReuseDisplacedBranchCallArgument",
            code,
            5
        );
    }

    private static byte[] Fixture(
        bool compacted = false,
        bool retryReadsOld = false,
        bool bypassCall = false
    )
    {
        int frame = compacted ? 2 : 3;
        byte value = Y(compacted ? 1 : 2);
        byte[] initialClear = compacted ? [] : [InitYRegisters, ExtendedListOperand, U(1), Y(2)];
        byte[] bypass = bypassCall ? [IsAtom, F(3), X(0)] : [];
        byte[] arguments = compacted
            ? [Move, Y(1), X(1), Move, X(0), Y(1), Swap, X(0), X(1)]
            : [
                Move, X(0), Y(2),
                Move, Y(2), X(1),
                Move, Y(1), X(0),
                InitYRegisters, ExtendedListOperand, U(1), Y(1)
            ];
        byte[] late = retryReadsOld ? [Move, Y(1), X(0)] : [Move, value, X(0)];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(frame), U(2), .. initialClear,
            Move, X(0), Y(0), Move, X(1), Y(1), .. bypass,
            LocalCall, U(2), F(2),
            .. arguments, Line, U(1), LocalCall, U(2), F(2),
            ExactEqualTest, F(3), X(0), A(1),
            Move, value, X(0), Deallocate, U(frame), Return,
            Label, U(3), .. late,
            InitYRegisters, ExtendedListOperand, U(2), Y(0), value,
            Deallocate, U(frame), Return,
            EndCode
        ];
    }
}
