using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamClearedUnaryCallResultChecks
{
    internal const string TestName = "compiler/beam/forwards-call-result-through-single-use-unary-bif";
    private const string IncorrectLayout = "A unary BIF should consume the ordinary call result directly only when the old slot stays dead.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_cleared_unary_call_result");
        if (Rewrite(Fixture()) is not { } changed || !changed.SequenceEqual(Fixture(compacted: true))
            || Rewrite(changed) is not null || Rewrite(Fixture(retryReadsSlot: true)) is not null
            || Rewrite(Fixture(guarded: true)) is not null || Rewrite(Fixture(cleared: false)) is not null)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        return Task.CompletedTask;

        byte[]? Rewrite(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "ForwardClearedCallResultToUnaryBif",
            code,
            4
        );
    }

    private static byte[] Fixture(
        bool compacted = false,
        bool retryReadsSlot = false,
        bool guarded = false,
        bool cleared = true
    )
    {
        byte[] initialize = cleared
            ? [InitYRegisters, ExtendedListOperand, U(1), Y(0)]
            : [Move, X(1), Y(0)];
        byte[] save = compacted ? [] : [Move, X(0), Y(0)];
        byte[] retry = retryReadsSlot ? [Move, Y(0), X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(1), U(2), .. initialize,
            Line, U(1), LocalCall, U(2), F(2),
            .. save, BifOne, F(guarded ? 3 : 0), U(0), compacted ? X(0) : Y(0), X(0),
            ExactEqualTest, F(3), X(0), A(1),
            Deallocate, U(1), Return,
            Label, U(3), .. retry, Deallocate, U(1), Return,
            EndCode
        ];
    }
}
