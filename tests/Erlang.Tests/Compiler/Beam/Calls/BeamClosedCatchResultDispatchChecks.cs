using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamClosedCatchResultDispatchChecks
{
    internal const string TestName = "compiler/beam/orders-disjoint-caught-tuple-and-atom-results";
    private const string WrongLayout = "A caught value must have a tuple test, a typed tag test, and a separate atom return.";

    internal static Task Run()
    {
        if (Rewrite(Fixture(ordered: false)) is not { } result || !result.SequenceEqual(Fixture(ordered: true))
            || Rewrite(Fixture(ordered: false, sharedArm: true)) is not null
            || Rewrite(Fixture(ordered: false, differentFrame: true)) is not null
            || Rewrite(result) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(bool ordered, bool sharedArm = false, bool differentFrame = false)
    {
        byte[] branch = sharedArm ? [IsInteger, F(4), X(0)] : [];
        byte[] taggedTail = [Move, A(1), X(0), TailExternalCallWithFrame, U(1), U(0), U(1)];
        byte[] fallbackTail = [Move, A(2), X(0), TailExternalCallWithFrame, U(1), U(0), U(differentFrame ? 2 : 1)];
        byte[] selection = ordered ?
            [
                IsTuple, F(3), X(0), IsTaggedTuple, F(4), ExtendedTypedRegisterOperand, X(0), U(1), U(2), A(2),
                .. taggedTail, Label, U(3), ExactEqualTest, F(4), X(0), A(1), Deallocate, U(1), Return,
                Label, U(4), .. fallbackTail
            ]
            :
            [
                ExactNotEqualTest, F(4), X(0), A(1), IsTaggedTuple, F(3), X(0), U(2), A(2),
                .. taggedTail, Label, U(3), .. fallbackTail, Label, U(4), Deallocate, U(1), Return
            ];

        return
        [
            .. Start(
                1,
                2,
                2,
                1
            ), Allocate, U(1), U(1), .. branch, CatchEnd, Y(0), .. selection, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] code)
    {
        var image = new ReadOnlyMemory<byte>(code);
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            5,
            1,
            image
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
        Func<int> type = () => 1;

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteClosedCatchResultDispatch",
            image,
            body,
            type
        );
    }
}
