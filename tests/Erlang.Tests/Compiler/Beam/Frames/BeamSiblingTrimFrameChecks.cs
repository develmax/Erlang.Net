using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamSiblingTrimFrameChecks
{
    internal const string TestName = "compiler/beam/compacts-an-owned-frame-beside-a-sibling-partial-trim";
    private const string WrongLayout = "Only the closed frame with an unambiguous identity may shrink.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_sibling_trim_frame_probe");
        byte[] original = Fixture();
        byte[] expected = Fixture(compacted: true);
        if (Rewrite(original) is not { } after || !after.AsSpan().SequenceEqual(expected)
            || Rewrite(after) is not null || Rewrite(Fixture(trimsSelectedFrame: true)) is not null
            || Rewrite(Fixture(joinsDistinctEpochs: true)) is not null
            || Rewrite(Fixture(completeSibling: true)) is not { } complete
            || !complete.AsSpan().SequenceEqual(Fixture(compacted: true, completeSibling: true))
            || Rewrite(complete) is not null
            || Rewrite(Fixture(joinsDistinctEpochs: true, completeSibling: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        byte[]? Rewrite(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "CompactFramesBesideSiblingTrims",
            code,
            7
        );
    }

    private static byte[] Fixture(
        bool compacted = false,
        bool trimsSelectedFrame = false,
        bool joinsDistinctEpochs = false,
        bool completeSibling = false
    )
    {
        int width = compacted ? 1 : 3;
        byte[] initialized = compacted ? [] : [InitYRegisters, ExtendedListOperand, U(2), Y(1), Y(2)];
        byte[] selectedReturn = trimsSelectedFrame
            ? [Move, Y(0), X(0), Move, X(0), Y(2), TrimFrame, U(1), U(2), Move, Y(1), X(0), Deallocate, U(2), Return]
            : [Move, Y(0), X(0), Deallocate, U(width), Return];
        int retained = completeSibling ? 0 : 1;
        byte[] trim = [TrimFrame, U(2 - retained), U(retained)];
        byte[] sibling = joinsDistinctEpochs
            ? [
                IsInteger, F(5), X(0), .. trim, Jump, F(6),
                Label, U(5), .. trim, Label, U(6)
            ]
            : trim;
        byte[] siblingResult = completeSibling ? [] : [Move, Y(0), X(0)];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            IsTuple, F(4), X(0), Allocate, U(width), U(1), Move, X(0), Y(0), .. initialized,
            IsAtom, F(3), X(0), Move, Y(0), X(0), Deallocate, U(width), Return,
            Label, U(3), .. selectedReturn,
            Label, U(4), Allocate, U(2), U(1), InitYRegisters, ExtendedListOperand, U(1), Y(0),
            Move, X(0), Y(1), .. sibling, .. siblingResult, Deallocate, U(retained), Return, EndCode
        ];
    }
}
