using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamRetiredConsClearChecks
{
    internal const string TestName = "compiler/beam/clears-a-retired-cons-field-on-its-own-path";
    private const string WrongLayout = "Only the path that wrote the retired slot needs its clear before the call.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_retired_cons_clear");
        _ = Call(compiler, "Intern", "keep");
        if (Apply(Fixture()) is not { } ordered || !ordered.SequenceEqual(Fixture(early: true))
            || Apply(ordered) is not null || Apply(Fixture(laterRead: true)) is not null
            || Apply(Fixture(incompletePath: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        byte[]? Apply(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "ClearRetiredConsSlotBeforeCall",
            code,
            5
        );
    }

    private static byte[] Fixture(bool early = false, bool laterRead = false, bool incompletePath = false)
    {
        byte[] clear = [InitYRegisters, ExtendedListOperand, U(1), Y(0)];
        byte[] more = laterRead ? [Move, Y(0), X(0)] : [];
        byte[] bypass = incompletePath ? [IsNil, F(4), X(0)] : [];
        byte[] onCall = early ? clear : [];
        byte[] atJoin = early ? [] : clear;

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(1), U(1), .. clear,
            ExactEqualTest, F(3), X(0), A(2), Move, X(0), Y(0), .. bypass,
            TestHeap, U(4), U(0), PutList, A(2), A(0), X(0), PutList, Y(0), X(0), X(0),
            .. onCall, Line, U(1), ExternalCall, U(1), U(0), .. more, Jump, F(4),
            Label, U(3), Move, A(2), X(0), Label, U(4), .. atJoin,
            Deallocate, U(1), Return, EndCode
        ];
    }
}
