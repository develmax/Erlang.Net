using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamOwnedNilRetryChecks
{
    internal const string TestName = "compiler/beam/keeps-nonempty-status-retry-in-fallthrough";
    private const string WrongLayout = "A retry may clear consumed roots only after its nil path has branched to the original value.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_owned_nil_retry");
        _ = Call(compiler, "Intern", "ok");
        if (Apply(Fixture()) is not ValueTuple<ReadOnlyMemory<byte>, int> ordered
            || ordered.Item2 != 6 || !ordered.Item1.Span.SequenceEqual(Fixture(ordered: true))
            || Apply(Fixture(readsOldRoot: true)) is not null
            || Apply(Fixture(otherFallback: true)) is not null
            || Apply(Fixture(hasOtherStatusEdge: false)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        object? Apply(ReadOnlyMemory<byte> code) => Call(
            compiler,
            "KeepNonemptyStatusRetryInFallthrough",
            code,
            7
        );
    }

    private static byte[] Fixture(
        bool ordered = false,
        bool readsOldRoot = false,
        bool otherFallback = false,
        bool hasOtherStatusEdge = true
    )
    {
        // The other status arm shares the original-value fallback with nil.
        byte[] statusEdge = hasOtherStatusEdge ? [ExactEqualTest, F(ordered ? 3 : 4), X(1), A(2)] : [];
        byte[] guard = ordered
            ? [ExactNotEqualTest, F(4), X(0), A(0)]
            : [IsNil, F(3), X(0), Move, Y(0), X(0), Jump, F(6), Label, U(3)];
        byte[] clears = ordered ? [U(3), Y(0), Y(1), Y(2)] : [U(1), Y(2)];
        byte[] extra = readsOldRoot ? [Move, Y(0), X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(3), U(2), InitYRegisters, ExtendedListOperand, U(3), Y(0), Y(1), Y(2),
            Move, X(1), Y(0), .. statusEdge, .. guard, TestHeap, U(4), U(0), PutList, Y(1), A(0), X(0), PutList, Y(2), X(0), X(0),
            InitYRegisters, ExtendedListOperand, .. clears, Line, U(1), ExternalCall, U(1), U(0), Move, I(1), X(1),
            Line, U(2), ExternalCall, U(2), U(1), Jump, F(ordered ? 5 : 6),
            Label, U(ordered ? 3 : 4), ExactNotEqualTest, F(ordered ? 5 : 6), X(0), A(2),
            Label, U(ordered ? 4 : 5), Move, Y(otherFallback ? 1 : 0), X(0),
            Label, U(ordered ? 5 : 6), Move, X(0), Y(1), .. extra, Move, Y(1), X(0), Deallocate, U(3), Return, EndCode
        ];
    }
}
