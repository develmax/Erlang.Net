using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamAdvancedOwnedPrefixTrimChecks
{
    internal const string TestName = "compiler/beam/advances-trim-over-a-cleared-prefix";
    private const string Pass = "AdvanceClearedOwnedPrefixTrim";
    private const string Failure = "The already-cleared prefix may be trimmed before the first call, while retaining the other roots.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("advance_owned_prefix_fixture");
        if (Rewrite(Fixture()) is not byte[] moved || !moved.SequenceEqual(Fixture(advanced: true))
            || Rewrite(Fixture(readPrefix: true)) is not null
            || Rewrite(Fixture(entry: true)) is not null
            || Rewrite(moved) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;

        byte[]? Rewrite(byte[] code) => (byte[]?)Call(
            compiler,
            Pass,
            new ReadOnlyMemory<byte>(code),
            5
        );
    }

    private static byte[] Fixture(bool advanced = false, bool readPrefix = false, bool entry = false)
    {
        byte[] release = advanced ? [TrimFrame, U(1), U(1)] : [InitYRegisters, ExtendedListOperand, U(1), Y(0)];
        byte[] boundary = entry ? [Label, U(3)] : [];
        byte[] use = readPrefix ? [Move, Y(0), X(1)] : [];
        byte[] redundant = advanced ? [] : [Move, X(0), Y(0), TrimFrame, U(1), U(1)];
        int retained = advanced ? 0 : 1;

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(2), U(2), Move, X(1), Y(0), Move, X(0), Y(1),
            Move, Y(0), X(1), .. release,
            Move, Y(retained), X(0), Line, U(1), ExternalCall, U(2), U(0),
            .. boundary, .. use,
            Move, Y(retained), X(0), Line, U(2), ExternalCall, U(1), U(1),
            .. redundant, Line, U(3), ExternalCall, U(1), U(2),
            Move, Y(0), X(0), Deallocate, U(1), Return, EndCode
        ];
    }
}
