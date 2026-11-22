using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamColdCheckedCaseExitChecks
{
    internal const string TestName = "compiler/beam/places-an-annotated-checked-failure-after-the-return";
    private const string WrongLayout = "The checked exit must stay isolated and retain its original source line.";

    internal static Task Run()
    {
        if (Rewrite(Fixture()) is not { } actual || !actual.AsSpan().SequenceEqual(Fixture(placed: true))
            || Rewrite(actual) is not null
            || Rewrite(Fixture(sharedSuccess: true)) is not { } shared
            || !shared.AsSpan().SequenceEqual(Fixture(placed: true, sharedSuccess: true))
            || Rewrite(Fixture(inheritedContinuationLine: true)) is not null || Rewrite(Fixture(bareFailure: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        if (Rewrite(NestedFixture(), "RewriteNestedIdentityCaseExits") is not { } scheduled
            || !scheduled.AsSpan().SequenceEqual(NestedFixture(placed: true))
            || Rewrite(scheduled, "RewriteNestedIdentityCaseExits") is not null
            || Rewrite(NestedFixture(callSiteLine: false), "RewriteNestedIdentityCaseExits") is not null
            || Rewrite(NestedFixture(sharedReject: true), "RewriteNestedIdentityCaseExits") is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(
        bool placed = false,
        bool sharedSuccess = false,
        bool inheritedContinuationLine = false,
        bool bareFailure = false
    )
    {
        byte[] failureLine = bareFailure ? [] : [Line, U(1)];
        byte[] failure = [Label, U(4), .. failureLine, CaseEnd, X(0)];
        byte[] suffix = placed ? failure : [];
        byte[] splice = placed
            ? (sharedSuccess ? [Label, U(5)] : [])
            : [Jump, F(5), .. failure, Label, U(5)];
        byte[] shared = sharedSuccess ? [ExactNotEqualTest, F(5), X(0), A(1)] : [];
        byte[] sourceLine = inheritedContinuationLine ? [] : [Line, U(2)];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(1), U(1), InitYRegisters, ExtendedListOperand, U(1), Y(0),
            LocalCall, U(1), F(8), IsTaggedTuple, F(4), X(0), U(2), A(1),
            GetTupleElement, X(0), U(1), X(0), .. shared, ExactEqualTest, F(3), X(0), A(2),
            Deallocate, U(1), Return,
            Label, U(3), .. splice, Move, X(0), X(1), .. sourceLine,
            CatchEnter, Y(0), F(6), Move, X(1), X(0), Label, U(6), CatchEnd, Y(0), Deallocate, U(1), Return,
            .. suffix,
            .. Start(
                7,
                8,
                2,
                1
            ),
            TestHeap, U(3), U(1), PutTuple, X(0), ExtendedListOperand, U(2), A(1), X(0), Return, EndCode
        ];
    }

    private static byte[] NestedFixture(bool placed = false, bool callSiteLine = true, bool sharedReject = false)
    {
        byte[] site = callSiteLine ? [Line, U(2)] : [];
        byte[] identity = [Label, U(7), Deallocate, U(1), Return];
        byte[] retry = [Label, U(4), Move, Y(0), X(0), TailLocalCallWithFrame, U(1), F(2), U(1)];
        byte[] nestedFailure = [Label, U(5), .. (placed ? site : []), CaseEnd, X(0)];
        byte[] outerFailure = [Label, U(6), Line, U(1), CaseEnd, X(0)];
        byte[] outerOther = sharedReject
            ? [Label, U(3), Jump, F(5)]
            : [Label, U(3), Move, A(1), X(0), Deallocate, U(1), Return];
        byte[] noFrame = [Label, U(8), Move, A(2), X(0), Return];
        byte[] layout = placed
            ? [.. identity, .. retry, .. outerOther, .. noFrame, .. nestedFailure, .. outerFailure]
            : [.. retry, .. nestedFailure, .. outerOther, .. outerFailure, .. identity, .. noFrame];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            IsTuple, F(8), X(0), Allocate, U(1), U(1), Move, X(0), Y(0),
            SelectValue, X(0), F(6), ExtendedListOperand, U(4), A(1), F(9), A(2), F(3),
            Label, U(9), .. site, ExternalCall, U(1), U(0),
            SelectValue, X(0), F(5), ExtendedListOperand, U(4), A(1), F(4), A(2), F(7),
            .. layout, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] bytes, string pass = "RewriteColdCheckedCaseExit")
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            10,
            2,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            pass,
            code,
            body
        );
    }
}
