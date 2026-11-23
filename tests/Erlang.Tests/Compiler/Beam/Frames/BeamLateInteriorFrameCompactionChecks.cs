using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamLateInteriorFrameCompactionChecks
{
    internal const string TestName = "compiler/beam/revisits-unused-interior-frame-slots";
    private const string WrongLayout = "An unobserved interior slot can be removed while a referenced or trimmed slot must keep its frame.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_late_interior_frame");
        if (Compact(compiler, Fixture()) is not { } result || !result.SequenceEqual(Fixture(compact: true))
            || Compact(compiler, Fixture(keepFirst: true)) is not null
            || Compact(compiler, Fixture(trimmed: true)) is not null
            || Compact(compiler, result) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(bool compact = false, bool keepFirst = false, bool trimmed = false)
    {
        int original = compact ? 2 : 3;
        int frame = trimmed ? 2 : original;
        byte first = compact || trimmed ? Y(0) : Y(1);
        byte second = compact || trimmed ? Y(1) : Y(2);
        byte[] fill = compact ? [] : [Move, X(0), Y(0)];
        byte[] shift = trimmed ? [TrimFrame, U(1), U(2)] : [];
        byte[] fields = keepFirst ? [Y(0), first, second] : [first, second];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(original), U(1),
            .. fill, Move, X(0), Y(compact ? 0 : 1), Move, X(0), Y(compact ? 1 : 2),
            ExternalCall, U(1), U(0), .. shift,
            TestHeap, U(fields.Length + 1), U(0),
            PutTuple, X(0), ExtendedListOperand, U(fields.Length), .. fields,
            Deallocate, U(frame), Return, EndCode
        ];
    }

    private static byte[]? Compact(BeamModuleCompiler compiler, ReadOnlyMemory<byte> code) =>
        (byte[]?)Call(
            compiler,
            "CompactLateUnusedInteriorSlots",
            code,
            3
        );
}
