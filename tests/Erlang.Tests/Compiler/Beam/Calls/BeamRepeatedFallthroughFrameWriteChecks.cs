using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamRepeatedFallthroughFrameWriteChecks
{
    internal const string TestName = "compiler/beam/keeps-one-copy-at-a-repeated-fallthrough-frame-write";
    private const string WrongLayout = "A repeated fallthrough save can disappear only if every incoming path keeps the join save.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_repeated_fallthrough_write");
        _ = Call(compiler, "Intern", "first");
        if (Apply(Fixture()) is not { } rewritten || !rewritten.SequenceEqual(Fixture(removed: true))
            || Apply(rewritten) is not null || Apply(Fixture(heapBoundary: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        byte[]? Apply(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "RemoveRepeatedFallthroughFrameWrites",
            code,
            5
        );
    }

    private static byte[] Fixture(bool removed = false, bool heapBoundary = false)
    {
        byte[] fallthrough = removed ? [] : [Move, X(0), Y(0)];
        byte[] boundary = heapBoundary ? [TestHeap, U(2), U(2)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(1), U(2), InitYRegisters, ExtendedListOperand, U(1), Y(0),
            ExactEqualTest, F(4), X(0), A(2),
            .. fallthrough, .. boundary,
            Label, U(3), Move, X(0), Y(0), Move, Y(0), X(0), Deallocate, U(1), Return,
            Label, U(4), Move, X(1), X(0), Jump, F(3), EndCode
        ];
    }
}
