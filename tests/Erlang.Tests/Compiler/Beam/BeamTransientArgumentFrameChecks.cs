using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTransientArgumentFrameChecks
{
    internal const string TestName = "compiler/beam/folds-transient-call-argument-slot";
    private const string WrongLayout = "A call argument may bypass its temporary frame slot only when the register copy still precedes the overwrite.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_transient_argument_frame");
        if (Apply(Fixture()) is not byte[] folded || !folded.AsSpan().SequenceEqual(Fixture(folded: true))
            || Apply(folded) is not null || Apply(Fixture(cycle: true)) is not null
            || Apply(Fixture(collects: true)) is not null || Apply(Fixture(unusedArgument: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        object? Apply(ReadOnlyMemory<byte> code) => Call(
            compiler,
            "FoldTransientArgumentFrameTrims",
            code,
            3
        );
    }

    private static byte[] Fixture(
        bool folded = false,
        bool cycle = false,
        bool collects = false,
        bool unusedArgument = false
    )
    {
        byte[] barrier = collects ? [TestHeap, U(2), U(4)] : [];
        byte[] prefix = folded
            ? [Allocate, U(0), U(4), Move, X(1), X(0), Move, X(3), X(1)]
            : [Allocate, U(1), U(4), Move, X(1), Y(0), .. barrier, Move, X(cycle ? 0 : 3), X(1),
                Move, Y(0), X(0), TrimFrame, U(1), U(0)];

        return
        [
            .. Start(
                1,
                2,
                1,
                4
            ), .. prefix, Line, U(1), ExternalCall, U(unusedArgument ? 1 : 2), U(0),
            Deallocate, U(0), Return, EndCode
        ];
    }
}
