using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamSharedCheckedCallSpillChecks
{
    internal const string TestName = "compiler/beam/shares-checked-call-result-spills-across-tag-check-edges";
    private const string Module = "beam_shared_checked_call_spill";
    private const string Pass = "ShareCheckedCallResultSpills";

    internal static Task Run()
    {
        byte[] input = Fixture();
        byte[] common = Fixture(shared: true);
        if (Rewrite(input) is not { } image || !image.SequenceEqual(common))
        {
            throw new InvalidOperationException(BeamSharedCheckedCallSpillDiagnostics.Shared);
        }

        CheckUnchanged(common);
        CheckUnchanged(Fixture(coldSlot: 1));
        CheckUnchanged(Fixture(afterProjection: [IsNil, F(3), X(1)]));
        CheckUnchanged(Fixture(beforeColdSpill: [Line, U(2)]));

        return Task.CompletedTask;
    }

    private static void CheckUnchanged(byte[] code)
    {
        if (Rewrite(code) is not null)
        {
            throw new InvalidOperationException(BeamSharedCheckedCallSpillDiagnostics.Unchanged);
        }
    }

    private static byte[]? Rewrite(ReadOnlyMemory<byte> code)
        => (byte[]?)Call(
            new BeamModuleCompiler(Module),
            Pass,
            code,
            4
        );

    private static byte[] Fixture(
        bool shared = false,
        int coldSlot = 0,
        byte[]? afterProjection = null,
        byte[]? beforeColdSpill = null
    )
    {
        byte[] spill = [Move, X(0), Y(0)];
        byte[] coldSpill = [Move, X(0), Y(coldSlot)];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(2), U(1),
            Move, X(0), Y(1),
            ExternalCall, U(1), U(0),
            .. (shared ? spill : []),
            IsTaggedTuple, F(3), X(0), U(2), A(1),
            .. (shared ? [] : spill),
            GetTupleElement, X(0), U(1), X(1),
            .. (afterProjection ?? []),
            Move, Y(0), X(0),
            Deallocate, U(2), Return,
            Label, U(3),
            .. (beforeColdSpill ?? []),
            .. (shared ? [] : coldSpill),
            Move, Y(coldSlot), X(0),
            Deallocate, U(2), Return,
            EndCode
        ];
    }
}
