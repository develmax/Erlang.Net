using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamConsumedPreCallRootsChecks
{
    internal const string TestName = "compiler/beam/retains-entry-roots-through-heap-before-the-first-call";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_consumed_precall_roots");
        byte[] original = Fixture();
        byte[] compact = Fixture(compact: true);
        if (Rewrite(compiler, original) is not { } actual || !compact.SequenceEqual(actual)
            || Rewrite(compiler, compact) is not null
            || Rewrite(compiler, Fixture(readAfterCall: true)) is not null
            || Rewrite(compiler, Fixture(clobber: true)) is not null
            || Rewrite(compiler, Fixture(register: 2)) is not null)
        {
            throw new InvalidOperationException(
                "A consumed frame alias must retain only a contiguous, unmodified X root through the collector."
            );
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(
        bool compact = false,
        bool readAfterCall = false,
        bool clobber = false,
        int register = 0
    )
    {
        byte[] save = compact ? [] : [Move, X(register), Y(0)];
        byte[] change = clobber ? [Move, A(1), X(0)] : [];
        byte[] read = readAfterCall ? [Move, Y(0), X(0)] : [];
        int width = compact ? 0 : 1;

        return
        [
            .. Start(
                1,
                2,
                1,
                register + 1
            ),
            Allocate, U(width), U(register + 1), .. save, .. change,
            TestHeap, U(3), U(compact ? 1 : 0),
            PutTuple, X(0), ExtendedListOperand, U(2), A(1), compact ? X(register) : Y(0),
            ExternalCall, U(1), U(0), .. read, Deallocate, U(width), Return,
            EndCode
        ];
    }

    private static byte[]? Rewrite(BeamModuleCompiler compiler, ReadOnlyMemory<byte> code) =>
        (byte[]?)Call(
            compiler,
            "EliminateConsumedPreCallRoots",
            code,
            3
        );
}
