using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamSharedStackCopyTailsChecks
{
    internal const string TestName = "compiler/beam/shares-owned-stack-copy-success-tails";
    private const string Module = "beam_shared_stack_copy";
    private const string Pass = "OrderFinalPhysicalLabels";

    internal static Task Run()
    {
        byte[] original = Fixture(ExactNotEqualTest);
        CheckShared(original, Fixture(ExactEqualTest, shared: true));
        CheckShared(Fixture(ExactEqualTest), Fixture(ExactNotEqualTest, shared: true));

        CheckUnchanged(Fixture(ExactNotEqualTest, laterCopy: [Move, Y(1), Y(0)]));
        CheckUnchanged(Fixture(ExactNotEqualTest, afterTest: [Line, U(6)]));
        CheckUnchanged(Fixture(ExactNotEqualTest, laterCopy: [Move, Y(0), Y(1), Line, U(6)]));
        CheckUnchanged(Fixture(ExactNotEqualTest, laterUsesJoin: false));
        CheckUnchanged(Fixture(ExactNotEqualTest, beforeTest: [DebugLine, A(1), U(1), U(1), U(1)]));

        return Task.CompletedTask;
    }

    private static void CheckShared(byte[] original, byte[] expected)
    {
        if (Rewrite(original, 6) is not { } result || result.Item2 != 7
            || !result.Item1.Span.SequenceEqual(expected))
        {
            throw new InvalidOperationException(BeamSharedStackCopyTailsDiagnostics.Shared);
        }

        if (Rewrite(expected, 7) is { } repeated && (repeated.Item2 != 7 || !repeated.Item1.Span.SequenceEqual(expected)))
        {
            throw new InvalidOperationException(BeamSharedStackCopyTailsDiagnostics.Stable);
        }
    }

    private static void CheckUnchanged(byte[] original)
    {
        if (Rewrite(original, 6) is { } result && (result.Item2 != 6 || !result.Item1.Span.SequenceEqual(original)))
        {
            throw new InvalidOperationException(BeamSharedStackCopyTailsDiagnostics.Excluded);
        }
    }

    private static (ReadOnlyMemory<byte>, int)? Rewrite(ReadOnlyMemory<byte> code, int labels)
        => ((ReadOnlyMemory<byte>, int)?)Call(
            new BeamModuleCompiler(Module),
            Pass,
            code,
            labels
        );

    private static byte[] Fixture(
        byte predicate,
        bool shared = false,
        byte[]? laterCopy = null,
        byte[]? afterTest = null,
        byte[]? beforeTest = null,
        bool laterUsesJoin = true
    )
    {
        int alternative = shared ? 5 : 4;
        int join = shared ? 6 : 5;
        byte[] copy = [Move, Y(0), Y(1)];
        byte[] firstSuccess = shared ? [] : [.. copy, Jump, F(join)];
        byte[] sharedLabel = shared ? [Label, U(4)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(2), U(2),
            Move, X(0), Y(0),
            Move, X(1), Y(1),
            Line, U(1),
            .. (beforeTest ?? []),
            predicate, F(shared ? 4 : 3), X(0), A(1),
            .. (afterTest ?? []),
            .. firstSuccess,
            Label, U(3),
            Line, U(2),
            IsNil, F(alternative), X(1),
            .. sharedLabel,
            .. (laterCopy ?? copy),
            Jump, F(laterUsesJoin ? join : alternative),
            Label, U(alternative),
            Line, U(3),
            Move, X(1), Y(1),
            Label, U(join),
            Line, U(4),
            Move, Y(1), X(0),
            Deallocate, U(2), Return,
            EndCode
        ];
    }
}
