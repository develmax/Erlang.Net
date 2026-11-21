using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCheckedCopiedTupleSourcesChecks
{
    internal const string TestName = "compiler/beam/prefers-live-x-in-copied-tuple-check-and-projection";
    private const string Module = "beam_checked_copied_tuple";
    private const string Pass = "PreferCopiedTupleSourcesInX";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler(Module);
        byte[] copy = [Move, X(0), Y(0)];
        byte[] resultCopy = [ExternalCall, U(1), U(0), .. copy];
        byte[] check = [IsTaggedTuple, F(3), Y(0), U(3), A(1)];
        byte[] checkedFromX = [IsTaggedTuple, F(3), X(0), U(3), A(1)];
        byte[] projection = [GetTupleElement, Y(0), U(1), X(0)];
        byte[] projectedFromX = [GetTupleElement, X(0), U(1), X(0)];
        byte[] comparison = [ExactEqualTest, F(4), X(0), A(1)];

        CheckRewrite(
            compiler,
            [.. resultCopy, .. check, .. projection, .. comparison],
            [.. resultCopy, .. checkedFromX, .. projectedFromX, .. comparison]
        );
        CheckRewrite(compiler, [.. copy, .. projection], [.. copy, .. projectedFromX]);

        CheckUnchanged(compiler, [.. resultCopy, .. check, Move, Y(1), X(0), .. projection, .. comparison]);
        CheckUnchanged(compiler, [.. resultCopy, .. check, Label, U(5), .. projection, .. comparison]);
        CheckUnchanged(compiler, [.. resultCopy, .. checkedFromX, .. projection, .. comparison]);
        CheckUnchanged(compiler, [.. resultCopy, .. check, GetTupleElement, Y(1), U(1), X(0), .. comparison]);
        CheckUnchanged(compiler, [.. resultCopy, .. check, GetTupleElement, Y(0), U(3), X(0), .. comparison]);
        CheckUnchanged(compiler, [.. resultCopy, .. check, GetTupleElement, Y(0), U(1), Y(1), .. comparison]);
        CheckUnchanged(compiler, [.. copy, .. check, .. projection, .. comparison]);
        CheckUnchanged(compiler, [.. resultCopy, .. check, .. projection]);
        CheckUnchanged(
            compiler,
            [.. resultCopy, .. check, .. projection, ExactEqualTest, F(4), Y(1), A(1)]
        );

        return Task.CompletedTask;
    }

    private static void CheckRewrite(BeamModuleCompiler compiler, byte[] original, byte[] expected)
    {
        byte[] expectedCode = Fixture(expected);
        if (Rewrite(compiler, Fixture(original)) is not { } rewritten || !rewritten.AsSpan().SequenceEqual(expectedCode))
        {
            throw new InvalidOperationException(BeamCheckedCopiedTupleSourcesDiagnostics.Rewrite);
        }

        if (Rewrite(compiler, expectedCode) is not null)
        {
            throw new InvalidOperationException(BeamCheckedCopiedTupleSourcesDiagnostics.AlreadyRewritten);
        }
    }

    private static void CheckUnchanged(BeamModuleCompiler compiler, byte[] body)
    {
        if (Rewrite(compiler, Fixture(body)) is not null)
        {
            throw new InvalidOperationException(BeamCheckedCopiedTupleSourcesDiagnostics.UnprovedCopy);
        }
    }

    private static byte[]? Rewrite(BeamModuleCompiler compiler, ReadOnlyMemory<byte> code)
        => (byte[]?)Call(
            compiler,
            Pass,
            code,
            6
        );

    private static byte[] Fixture(byte[] body)
        =>
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(2), U(2),
            Move, X(1), Y(1),
            .. body,
            Deallocate, U(2), Return,
            Label, U(3),
            Deallocate, U(2), Return,
            Label, U(4),
            Move, Y(0), X(0),
            Deallocate, U(2), Return,
            EndCode
        ];
}
