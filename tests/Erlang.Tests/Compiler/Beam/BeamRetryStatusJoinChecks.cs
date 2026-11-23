using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamRetryStatusJoinChecks
{
    internal const string TestName = "compiler/beam/keeps-joined-retry-status-in-its-return-slot";
    private const string WrongLayout = "The retry status must enter its final slot on both value edges and preserve the terminal effect and failure.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_retry_status_join");
        _ = Call(compiler, "Intern", "ok");
        _ = Call(compiler, "Intern", "error");
        _ = Call(compiler, "Intern", "message");
        if (Apply(Fixture(), 8) is not ValueTuple<ReadOnlyMemory<byte>, int> ordered
            || ordered.Item2 != 9 || !ordered.Item1.Span.SequenceEqual(Fixture(ordered: true))
            || Apply(ordered.Item1, 9) is not null || Apply(Fixture(extraFailureEntry: true), 8) is not null
            || Apply(Fixture(differentSuccessAtom: true), 8) is not null || Apply(Fixture(missingRoot: true), 8) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        object? Apply(ReadOnlyMemory<byte> code, int labels) => Call(
            compiler,
            "RetainRetryStatusInFinalSlot",
            code,
            labels
        );
    }

    private static byte[] Fixture(
        bool ordered = false,
        bool extraFailureEntry = false,
        bool differentSuccessAtom = false,
        bool missingRoot = false
    )
    {
        byte failure = F(ordered ? 8 : 7);
        byte[] rootClear = missingRoot ? [U(2), Y(1), Y(2)] : [U(3), Y(0), Y(1), Y(2)];
        byte[] callResult = ordered ? [Move, X(0), Y(2)] : [];
        byte[] restore = ordered ? [Move, Y(0), Y(2)] : [Move, Y(0), X(0)];
        byte[] join = ordered ? [] : [Move, X(0), Y(1)];
        byte status = Y(ordered ? 2 : 1);
        byte[] trim = ordered ? [] : [Move, Y(1), Y(2), TrimFrame, U(2), U(1)];
        byte[] sharedAtom = ordered ? [Label, U(7)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(3), U(1), InitYRegisters, ExtendedListOperand, U(3), Y(0), Y(1), Y(2), Move, X(0), Y(0),
            IsTuple, extraFailureEntry ? failure : F(3), X(0),
            ExactNotEqualTest, F(4), X(0), A(0), TestHeap, U(4), U(0), PutList, Y(1), A(0), X(0),
            PutList, Y(2), X(0), X(0), InitYRegisters, ExtendedListOperand, .. rootClear,
            Line, U(1), ExternalCall, U(1), U(0), Move, I(1), X(1), Line, U(2), ExternalCall, U(2), U(1),
            .. callResult, Jump, F(5),
            Label, U(3), ExactNotEqualTest, F(ordered ? 7 : 5), X(0), A(differentSuccessAtom ? 3 : 2),
            Label, U(4), .. restore, Label, U(5), .. join, IsTuple, F(6), status,
            IsTaggedTuple, failure, ExtendedTypedRegisterOperand, status, U(1), U(2), A(3),
            GetTupleElement, status, U(1), X(0), IsTuple, failure, X(0), TestArity, failure, X(0), U(2),
            TestHeap, U(4), U(1), GetTupleElement, X(0), U(0), X(1), GetTupleElement, X(0), U(1), X(0),
            PutList, X(0), A(0), X(0), PutList, X(1), X(0), X(1), Move, A(4), X(0), .. trim,
            Line, U(3), ExternalCall, U(2), U(2), Move, Y(ordered ? 2 : 0), X(0),
            Deallocate, U(ordered ? 3 : 1), Return,
            Label, U(6), ExactEqualTest, failure, status, A(2), .. sharedAtom,
            Move, A(2), X(0), Deallocate, U(3), Return, Label, U(ordered ? 8 : 7), Line, U(4), CaseEnd, status, EndCode
        ];
    }
}
