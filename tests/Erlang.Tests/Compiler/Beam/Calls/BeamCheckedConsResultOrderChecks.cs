using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCheckedConsResultOrderChecks
{
    internal const string TestName = "compiler/beam/prefers-the-checked-cons-result-before-empty-result-copies";
    private const string RewriteMethod = "RewriteCheckedConsResultOrder";
    private const string ResultOrderError = "A constructor branch changed an incoming edge or its collecting operation line.";

    internal static void Run()
    {
        byte[] original = Build(reordered: false, shareRetry: false, hasLine: true);
        byte[] expected = Build(reordered: true, shareRetry: false, hasLine: true);
        byte[] shared = Build(reordered: false, shareRetry: true, hasLine: true);
        byte[] missingLine = Build(reordered: false, shareRetry: false, hasLine: false);
        if (Rewrite(original) is not { } rewritten || !rewritten.SequenceEqual(expected)
            || Rewrite(expected) is not null || Rewrite(shared) is not null || Rewrite(missingLine) is not null)
        {
            throw new InvalidOperationException(ResultOrderError);
        }
    }

    private static byte[] Build(bool reordered, bool shareRetry, bool hasLine)
    {
        byte[] shared = shareRetry ? [ExactEqualTest, F(3), X(0), I(1)] : [];
        byte[] header = [.. Start(
            1,
            2,
            1,
            1
        ), Allocate, U(2), U(1),
            InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1), .. shared];
        byte[] coldLine = hasLine ? [Line, U(1)] : [];
        byte[] body =
        [
            GetList, X(0), X(0), X(1),
            .. coldLine,
            GcBifTwo, F(0), U(2), U(0), X(0), I(1), X(2),
            Move, X(1), Y(0),
            Move, X(2), Y(1)
        ];
        byte[] empty = [Move, A(0), Y(0), Move, I(1), Y(1)];
        byte[] suffix = [Label, U(4), Move, Y(0), X(0), Deallocate, U(2), Return, Label, U(5), CaseEnd, X(0), EndCode];

        return reordered
            ? [.. header, IsNonemptyList, F(3), X(0), .. body, Jump, F(4), Label, U(3), IsNil, F(5), X(0), .. empty, .. suffix]
            : [.. header, IsNil, F(3), X(0), .. empty, Jump, F(4), Label, U(3), IsNonemptyList, F(5), X(0), .. body, .. suffix];
    }

    private static byte[]? Rewrite(byte[] bytes)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            6,
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
            RewriteMethod,
            code,
            body
        );
    }
}
