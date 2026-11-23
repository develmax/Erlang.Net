using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamBooleanMembershipGuardChecks
{
    internal const string TestName = "compiler/beam/coalesces-adjacent-exhaustive-boolean-guards";
    private const string WrongGuard = "Boolean membership must preserve its rejection edge and the source register.";

    internal static Task Run()
    {
        foreach (byte operand in new[] { X(0), Y(0) })
        {
            if (Combine(Fixture(operand)) is not { } optimized
                || !optimized.SequenceEqual(Fixture(operand, lowered: true))
                || (operand == Y(0) && Combine(Fixture(operand, mixed: true)) is not null))
            {
                throw new InvalidOperationException(WrongGuard);
            }
        }

        if (Combine(Fixture(X(0), mixed: true)) is not { } dispatch
            || !dispatch.SequenceEqual(Fixture(X(0), lowered: true, mixed: true))
            || Combine(Fixture(X(0), mixed: true, shared: true)) is not null)
        {
            throw new InvalidOperationException(WrongGuard);
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(
        byte operand,
        bool lowered = false,
        bool mixed = false,
        bool shared = false
    )
    {
        byte[] test = lowered
            ? [IsBoolean, F(4), operand]
            : [ExactNotEqualTest, F(3), operand, A(3), ExactEqualTest, F(4), operand, A(4)];
        if (mixed && lowered)
        {
            test =
            [
                SelectValue, operand, F(5), ExtendedListOperand, U(6),
                A(5), F(4), A(4), F(3), A(3), F(3)
            ];
        }

        byte[] accepted = shared ? [Jump, F(4)] : [Return];
        byte[] rejected = mixed && !lowered
            ? [ExactEqualTest, F(5), operand, A(5), Return]
            : [Return];

        return
        [
            .. Start(
                1,
                2,
                2,
                1
            ),
            .. test, Label, U(3), .. accepted, Label, U(4), .. rejected,
            Label, U(5), Return, EndCode
        ];
    }

    private static byte[]? Combine(byte[] value)
    {
        var image = new ReadOnlyMemory<byte>(value);
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            6,
            1,
            image
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            6,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            new BeamModuleCompiler("beam_boolean_membership"),
            "CombineAtomMembershipTests",
            image,
            body,
            new (int, int, int)[] { (0, 0, 4) },
            new[] { "beam_boolean_membership", "run", "true", "false", "additional" },
            false
        );
    }
}
