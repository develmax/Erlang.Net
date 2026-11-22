using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamColdProtectedProjectionFailureChecks
{
    internal const string TestName = "compiler/beam/outlines-owned-projection-badmatches-in-otp-order";
    private const string WrongLayout = "Cold block relocation must preserve failed values and the protected success path.";

    internal static Task Run()
    {
        if (Rewrite(Fixture()) is not { } actual || !actual.SequenceEqual(Fixture(outlined: true))
            || Rewrite(actual) is not null || Rewrite(Fixture(secondReadsY: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(bool outlined = false, bool secondReadsY = false)
    {
        byte[] tupleFailure = [Label, U(3), Line, U(1), BadMatch, X(0)];
        byte[] listFailure = [Label, U(5), Line, U(2), BadMatch, secondReadsY ? Y(1) : X(0)];
        byte[] tupleRelay = outlined ? [] : [Jump, F(4), .. tupleFailure, Label, U(4)];
        byte[] listRelay = outlined ? [] : [Jump, F(6), .. listFailure, Label, U(6)];
        byte[] cold = outlined ? [.. listFailure, .. tupleFailure] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                0
            ),
            Allocate, U(2), U(0), InitYRegisters, ExtendedListOperand, U(1), Y(1), TryEnter, Y(0), F(7),
            ExternalCall, U(0), U(0), IsTuple, F(3), X(0), TestArity, F(3), X(0), U(2),
            GetTupleElement, X(0), U(1), X(1), .. tupleRelay,
            Move, A(1), X(0), ExternalCall, U(2), U(1), IsNonemptyList, F(5), X(0),
            GetList, X(0), Y(1), X(1), IsNil, F(5), X(1), .. listRelay,
            Move, Y(1), X(0), TryEnd, Y(0), Deallocate, U(2), Return,
            Label, U(7), TryCase, Y(0), Move, A(1), X(0), Deallocate, U(2), Return,
            .. cold, EndCode
        ];
    }

    private static byte[]? Rewrite(ReadOnlyMemory<byte> code)
    {
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            8,
            1,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            2,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteColdProtectedProjectionFailures",
            code,
            body
        );
    }
}
