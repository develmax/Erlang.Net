using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamAliasedTupleGuardElementChecks
{
    internal const string TestName = "compiler/beam/projects-elements-from-the-checked-guard-carrier";
    private const string WrongLayout = "Guard element lowering changed an unproved tuple read or kept a redundant failure edge.";

    internal static Task Run()
    {
        byte[] original = Image(projected: false);
        byte[] expected = Image(projected: true);
        if (Rewrite(original) is not { } actual || !actual.SequenceEqual(expected)
            || Rewrite(Image(projected: false, invalidIndex: true)) is not null
            || Rewrite(Image(projected: false, differentCarrier: true)) is not null
            || Rewrite(Image(projected: false, writesCarrier: true)) is not null
            || Rewrite(expected) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Image(
        bool projected,
        bool invalidIndex = false,
        bool differentCarrier = false,
        bool writesCarrier = false
    )
    {
        byte proof = differentCarrier ? Y(1) : Y(0);
        byte spill = writesCarrier ? Y(0) : Y(1);
        byte[] first = projected
            ? [GetTupleElement, Y(0), U(0), X(0)]
            : [BifTwo, F(3), U(0), invalidIndex ? I(3) : I(1), Y(0), X(0)];
        byte[] second = projected
            ? [GetTupleElement, Y(0), U(1), X(0)]
            : [BifTwo, F(3), U(0), I(2), Y(0), X(0)];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(2), U(1),
            InitYRegisters, ExtendedListOperand, U(1), Y(1),
            Move, X(0), Y(0), Move, proof, X(0),
            IsTuple, F(3), X(0), TestArity, F(3), X(0), U(2),
            .. first, Move, X(0), spill, .. second,
            GcBifTwo, F(3), U(1), U(1), Y(1), X(0), X(0),
            Deallocate, U(2), Return,
            Label, U(3), Move, A(0), X(0), Deallocate, U(2), Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] code)
    {
        var image = new ReadOnlyMemory<byte>(code);
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            4,
            1,
            image
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
            "RewriteAliasedTupleGuardElements",
            image,
            body,
            0,
            1
        );
    }
}
