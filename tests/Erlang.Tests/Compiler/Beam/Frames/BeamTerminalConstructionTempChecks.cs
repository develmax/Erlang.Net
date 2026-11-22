using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTerminalConstructionTempChecks
{
    internal const string TestName = "compiler/beam/keeps-terminal-construction-intermediates-in-x";
    private const string WrongLayout = "A construction intermediate may leave its slot only within a closed reserved heap region.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_terminal_construction_temp");
        if (Promote(compiler, Fixture()) is not { } actual || !actual.SequenceEqual(Fixture(compact: true))
            || Promote(compiler, Fixture(clobber: true)) is not null
            || Promote(compiler, actual) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(bool compact = false, bool clobber = false)
    {
        int width = compact ? 1 : 2;
        byte incoming = compact ? Y(0) : Y(1);
        byte retained = compact ? X(0) : Y(0);
        byte[] init = compact ? [] : [InitYRegisters, ExtendedListOperand, U(1), Y(0)];
        byte[] store = compact ? [] : [Move, X(0), Y(0)];
        byte[] other = clobber ? [ExternalCall, U(0), U(0), TestHeap, U(3), U(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(width), U(1), .. init, Move, X(0), incoming, ExternalCall, U(1), U(0),
            TestHeap, U(5), U(1), PutTuple, X(0), ExtendedListOperand, U(1), X(0), .. store, .. other,
            PutTuple, X(0), ExtendedListOperand, U(2), retained, incoming,
            Deallocate, U(width), Return, EndCode
        ];
    }

    private static byte[]? Promote(BeamModuleCompiler owner, ReadOnlyMemory<byte> code) =>
        (byte[]?)Call(
            owner,
            "PromoteTerminalConstructionTemps",
            code,
            3
        );
}
