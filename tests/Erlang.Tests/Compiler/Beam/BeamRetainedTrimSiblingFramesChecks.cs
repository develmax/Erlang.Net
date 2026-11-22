using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamRetainedTrimSiblingFramesChecks
{
    internal const string TestName = "compiler/beam/compacts-single-retained-trim-beside-full-frame-branch";
    private const string WrongFrame = "A one-slot trim and its full-frame sibling must preserve their own frame widths.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("retained_trim_sibling_probe");
        _ = Call(compiler, "Intern", "success");
        ReadOnlyMemory<byte> original = Fixture();
        var compact = (byte[]?)Call(
            compiler,
            "CompactRetainedTrimSiblingFrames",
            original,
            5
        );
        if (compact is null || !compact.SequenceEqual(Fixture(compacted: true))
            || Apply(Fixture(joinEpochs: true)) is not null)
        {
            throw new InvalidOperationException(WrongFrame);
        }

        CheckLoadEntry(compiler);

        return Task.CompletedTask;

        byte[]? Apply(ReadOnlyMemory<byte> image) => (byte[]?)Call(
            compiler,
            "CompactRetainedTrimSiblingFrames",
            image,
            5
        );
    }

    private static void CheckLoadEntry(BeamModuleCompiler compiler)
    {
        if (Apply(LoadFixture()) is not { } compacted || compacted[8] != OnLoad || compacted[10] != U(4)
            || !compacted.AsSpan().Contains(TrimFrame) || Apply(LoadFixture(joinEpochs: true)) is not null)
        {
            throw new InvalidOperationException(WrongFrame);
        }

        byte[]? Apply(ReadOnlyMemory<byte> image) => (byte[]?)Call(
            compiler,
            "CompactLoadEntryTrimSiblingFrame",
            image,
            5
        );
    }

    private static byte[] LoadFixture(bool joinEpochs = false)
    {
        byte[] exit = joinEpochs
            ? [Jump, F(4)]
            : [Move, Y(0), X(0), Deallocate, U(1), Return];

        return
        [
            .. Start(
                1,
                2,
                1,
                0
            ),
            OnLoad, Allocate, U(6), U(0), Move, A(2), Y(0),
            InitYRegisters, ExtendedListOperand, U(5), Y(1), Y(2), Y(3), Y(4), Y(5),
            ExternalCall, U(0), U(0), Move, X(0), Y(1), ExternalCall, U(0), U(1),
            GetTupleElement, X(0), U(0), Y(2), GetTupleElement, X(0), U(1), Y(3),
            Move, Y(2), X(0), Move, Y(3), X(1), ExternalCall, U(2), U(2),
            Move, X(0), Y(4), IsTaggedTuple, F(4), X(0), U(2), A(2),
            Move, Y(0), X(0), Move, Y(1), X(1), ExternalCall, U(2), U(3), Move, X(0), Y(5),
            ExactEqualTest, F(3), X(0), A(2), Move, Y(4), X(0), Deallocate, U(6), Return,
            Label, U(3), Move, Y(4), X(0), Move, A(2), X(1), TrimFrame, U(5), U(1),
            ExternalCall, U(2), U(4), .. exit,
            Label, U(4), Move, Y(4), X(0), Deallocate, U(6), Return, EndCode
        ];
    }

    private static byte[] Fixture(bool compacted = false, bool joinEpochs = false)
    {
        byte width = U(compacted ? 4 : 6);
        byte first = Y(compacted ? 3 : 0);
        byte second = Y(compacted ? 2 : 1);
        byte field1 = Y(compacted ? 1 : 2);
        byte field2 = Y(compacted ? 0 : 3);
        byte retained = Y(compacted ? 1 : 4);
        byte status = Y(compacted ? 3 : 5);
        byte[] init = compacted
            ? [InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1)]
            : [InitYRegisters, ExtendedListOperand, U(4), Y(2), Y(3), Y(4), Y(5)];
        byte[] saves = compacted
            ? [Move, X(1), second, Move, X(0), first]
            : [Move, X(0), first, Move, X(1), second];
        byte[] clear = compacted
            ? [InitYRegisters, ExtendedListOperand, U(2), field2, field1]
            : [InitYRegisters, ExtendedListOperand, U(2), field1, field2];
        byte[] finalReturn = joinEpochs
            ? [Jump, F(4)]
            : [Move, Y(0), X(0), Deallocate, U(1), Return];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, width, U(2), .. init, .. saves,
            ExternalCall, U(2), U(0),
            GetTupleElement, X(0), U(0), field1,
            GetTupleElement, X(0), U(1), field2,
            Move, field1, X(0), Move, field2, X(1), .. clear,
            ExternalCall, U(2), U(1),
            Move, X(0), retained,
            IsTaggedTuple, F(4), X(0), U(2), A(2),
            Move, first, X(0), Move, second, X(1),
            ExternalCall, U(2), U(2),
            Move, X(0), status,
            ExactEqualTest, F(3), X(0), A(2),
            Move, retained, X(0), Deallocate, width, Return,
            Label, U(3),
            Move, retained, X(0), TrimFrame, U(compacted ? 3 : 5), U(1),
            ExternalCall, U(1), U(3), .. finalReturn,
            Label, U(4), Move, retained, X(0), Deallocate, width, Return,
            EndCode
        ];
    }
}
