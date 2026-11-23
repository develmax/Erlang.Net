using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTaggedGuardRetryChecks
{
    internal const string TestName = "compiler/beam/shares-a-checked-tuple-across-a-guard-retry";
    private const string Failure = "The retry must reuse a tag check only when its predecessors preserve the selection tuple.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_tagged_guard_retry");
        byte[] prefix = Start(
            1,
            2,
            1,
            2
        );
        byte[] first =
        [
            GetTupleElement, X(0), U(1), X(2), ExactEqualTest, F(3), X(1), I(7), Move, X(2), X(0), Return
        ];
        byte[] second = [Move, I(2), X(0), Return];
        byte[] fallback = [Label, U(4), Move, I(3), X(0), Return, EndCode];
        byte[] code =
        [
            .. prefix, IsTaggedTuple, F(3), X(0), U(2), A(1), .. first,
            Label, U(3), IsTaggedTuple, F(4), X(0), U(2), A(1), .. second, .. fallback
        ];
        byte[] expected =
        [
            .. prefix, IsTaggedTuple, F(4), X(0), U(2), A(1), .. first,
            Label, U(3), .. second, .. fallback
        ];
        byte[] otherEntry =
        [
            .. prefix, IsAtom, F(3), X(1), IsTaggedTuple, F(3), X(0), U(2), A(1), .. first,
            Label, U(3), IsTaggedTuple, F(4), X(0), U(2), A(1), .. second, .. fallback
        ];
        byte[] clobbered =
        [
            .. prefix, IsTaggedTuple, F(3), X(0), U(2), A(1), GetTupleElement, X(0), U(1), X(0),
            ExactEqualTest, F(3), X(1), I(7), Return,
            Label, U(3), IsTaggedTuple, F(4), X(0), U(2), A(1), .. second, .. fallback
        ];
        if (Rewrite(code) is not { } actual || !actual.SequenceEqual(expected)
            || Rewrite(expected) is not null || Rewrite(otherEntry) is not null || Rewrite(clobbered) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;

        byte[]? Rewrite(ReadOnlyMemory<byte> input) => (byte[]?)Call(
            compiler,
            "ShareCheckedTaggedTupleGuardRetries",
            input,
            5
        );
    }
}
