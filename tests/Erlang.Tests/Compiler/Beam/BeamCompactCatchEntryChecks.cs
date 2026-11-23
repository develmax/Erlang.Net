using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCompactCatchEntryChecks
{
    internal const string TestName = "compiler/beam/drops-immediately-overwritten-catch-entry-roots";
    private const string Failure = "Catch setup must keep all unchanged input registers and omit only overwritten roots.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_compact_catch_entry_probe");
        _ = Call(compiler, "Intern", "extra");
        byte[] before = Fixture();

        if (Rewrite(before) is not { } after || !after.SequenceEqual(Fixture(compacted: true))
            || Rewrite(Fixture(reload: 0)) is not null || Rewrite(Fixture(extra: 1)) is not null
            || Rewrite(Fixture(initialized: 2)) is not null || Rewrite(after) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;

        byte[]? Rewrite(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "CompactClosedCatchEntryInputs",
            code,
            4
        );
    }

    private static byte[] Fixture(
        bool compacted = false,
        int reload = 1,
        int extra = 2,
        int initialized = 0
    ) =>
    [
        .. Start(
            1,
            2,
            1,
            2
        ),
        Allocate, U(3), U(2),
        .. (compacted ? new byte[] { Move, X(1), Y(0), Move, X(0), Y(1) } :
            [InitYRegisters, ExtendedListOperand, U(1), Y(initialized), Move, X(0), Y(1), Move, X(1), Y(0)]),
        CatchEnter, Y(2), F(3), Move, A(2), X(extra),
        .. (compacted ? new byte[] { } : [Move, Y(reload), X(0)]),
        Line, U(1), ExternalCall, U(3), U(0),
        Label, U(3), CatchEnd, Y(2), Deallocate, U(3), Return, EndCode
    ];
}
