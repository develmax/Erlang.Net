using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCheckedPayloadCallArgumentChecks
{
    internal const string TestName = "compiler/beam/forwards-a-checked-payload-before-an-owned-catch";
    private const string WrongLayout = "The checked argument must retain its caller value and clear the retired root.";

    internal static Task Run()
    {
        byte[] expected = Fixture(forwarded: true);
        byte[] beforeEquality = Fixture(beforeEquality: true);
        if (Rewrite(beforeEquality) is not null
            || RewriteAfterCheckedEquality(beforeEquality) is not { } normalized
            || !normalized.AsSpan().SequenceEqual(expected)
            || Rewrite(Fixture()) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Rewrite(actual) is not null || Rewrite(Fixture(readsRetired: true)) is not null
            || Rewrite(Fixture(bypassCheck: true)) is not null || Rewrite(Fixture(clobbersResult: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(
        bool forwarded = false,
        bool readsRetired = false,
        bool bypassCheck = false,
        bool clobbersResult = false,
        bool beforeEquality = false
    )
    {
        byte[] store = forwarded ? [] : [Move, X(0), Y(0)];
        byte[] reload = forwarded
            ? [Move, X(0), X(2), InitYRegisters, ExtendedListOperand, U(1), Y(0)]
            : [Move, Y(0), X(2)];
        byte[] clobber = clobbersResult ? [Move, A(2), X(0)] : [];
        byte[] observe = readsRetired ? [Move, Y(0), X(1)] : [];
        byte projected = X(beforeEquality ? 1 : 0);
        byte[] secondRead = beforeEquality ? [GetTupleElement, X(0), U(1), X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(1), U(1), Move, X(0), Y(0), LocalCall, U(1), F(8),
            IsTaggedTuple, F(bypassCheck ? 5 : 4), X(0), U(2), A(1),
            GetTupleElement, X(0), U(1), projected, ExactEqualTest, F(3), projected, A(2),
            Move, A(3), X(0), Deallocate, U(1), Return,
            Label, U(3), .. secondRead, Jump, F(5), Label, U(4), Line, U(1), CaseEnd, X(0),
            Label, U(5), .. store, Move, I(1), X(1), .. clobber, .. reload, Move, A(3), X(0),
            Line, U(2), LocalCall, U(3), F(10), ExactEqualTest, F(6), X(0), A(1),
            Deallocate, U(1), Return,
            Label, U(6), .. observe, CatchEnter, Y(0), F(11),
            Move, A(2), X(0), Label, U(11), CatchEnd, Y(0),
            Deallocate, U(1), Return,
            .. Start(
                7,
                8,
                2,
                1
            ),
            TestHeap, U(3), U(1), PutTuple, X(0), ExtendedListOperand, U(2), A(1), X(0), Return,
            .. Start(
                9,
                10,
                3,
                3
            ),
            Move, X(2), X(0), Return, EndCode
        ];
    }

    private static byte[]? RewriteAfterCheckedEquality(byte[] bytes)
    {
        var compiler = new BeamModuleCompiler("beam_checked_payload_composition");
        _ = Call(compiler, "Intern", "comparison");
        _ = Call(compiler, "Intern", "alternate");
        ReadOnlyMemory<byte> code = bytes;

        return (byte[]?)Call(
            compiler,
            "FinishCheckedEqualityProjection",
            code,
            12
        );
    }

    private static byte[]? Rewrite(byte[] bytes)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            12,
            3,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            4,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteCheckedPayloadCallArgument",
            code,
            body
        );
    }
}
