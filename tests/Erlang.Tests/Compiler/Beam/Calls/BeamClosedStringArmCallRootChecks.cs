using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamClosedStringArmCallRootChecks
{
    internal const string TestName = "compiler/beam/clears-a-frame-root-only-in-the-accepting-string-arm";
    private const string RewriteMethod = "RewriteClosedStringArmCallRoot";
    private const string IncorrectRoot = "A closed string arm cleared the wrong frame root.";

    internal static void Run()
    {
        byte[] original = Create(clear: false, keep: false);
        byte[] expected = Create(clear: true, keep: false);
        byte[] retained = Create(clear: false, keep: true);
        if (Rewrite(original) is not { } actual || !actual.SequenceEqual(expected)
            || Rewrite(expected) is not null || Rewrite(retained) is not null)
        {
            throw new InvalidOperationException(IncorrectRoot);
        }
    }

    private static byte[] Create(bool clear, bool keep)
    {
        byte[] root = clear ? [InitYRegisters, ExtendedListOperand, U(1), Y(3)] : [];
        byte[] extraUse = keep ? [Move, Y(3), X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                3
            ),
            Allocate, U(4), U(3),
            InitYRegisters, ExtendedListOperand, U(4), Y(0), Y(1), Y(2), Y(3),
            ExactEqualTest, F(6), X(0), ExtendedLiteralOperand, U(0),
            .. root,
            Move, Y(0), X(0),
            Line, U(1),
            ExternalCall, U(1), U(0),
            SelectValue, X(0), F(5), ExtendedListOperand, U(4), A(2), F(4), A(3), F(3),
            Label, U(3),
            TestHeap, U(2), U(0),
            PutList, Y(1), Y(2), X(1),
            Move, ExtendedLiteralOperand, U(0), X(2),
            Move, Y(2), X(0),
            TailLocalCallWithFrame, U(3), F(2), U(4),
            Label, U(4),
            .. extraUse,
            Deallocate, U(4),
            Return,
            Label, U(5),
            CaseEnd, X(0),
            Label, U(6),
            TestHeap, U(2), U(0),
            PutList, Y(1), Y(2), X(1),
            Move, Y(3), X(2),
            Move, Y(2), X(0),
            TailLocalCallWithFrame, U(3), F(2), U(4),
            EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] bytes)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            7,
            1,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            1,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            RewriteMethod,
            code,
            body,
            2,
            3
        );
    }
}
