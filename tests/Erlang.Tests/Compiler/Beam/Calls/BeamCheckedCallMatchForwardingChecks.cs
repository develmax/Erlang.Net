using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCheckedCallMatchForwardingChecks
{
    internal const string TestName = "compiler/beam/forwards-protected-call-result-through-checked-projection";
    private const string Module = "beam_checked_call_match_forwarding_fixture";
    private const string WrongLayout = "The match must preserve the original failed value and the successful outgoing argument.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler(Module);
        foreach (bool tuple in new[] { false, true })
        {
            if (Rewrite(compiler, Fixture(tuple)) is not { } actual
                || !actual.SequenceEqual(Fixture(tuple, forward: true))
                || Rewrite(compiler, actual) is not null
                || Rewrite(compiler, Fixture(tuple, liveCarrier: true)) is not null)
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(bool tuple, bool forward = false, bool liveCarrier = false)
    {
        const int Failure = 3;
        const int Success = 4;
        const int Handler = 5;
        byte carrier = forward ? X(0) : Y(1);
        byte projected = forward ? X(1) : Y(2);
        byte[] spill = forward ? [] : [Move, X(0), Y(1)];
        byte[] restore = forward ? [] : [Move, Y(1), X(0)];
        byte[] live = liveCarrier ? [Move, Y(1), X(0)] : [];
        byte[] checks = tuple
            ? [IsTuple, F(Failure), carrier, TestArity, F(Failure), carrier, U(2),
                GetTupleElement, carrier, U(1), projected]
            : [IsNonemptyList, F(Failure), carrier, GetList, carrier, Y(3), projected,
                IsNil, F(Failure), projected];
        byte[] outgoing = tuple
            ? [.. (forward ? [] : new byte[] { Move, Y(2), X(1) }), Move, A(1), X(0)]
            : [Move, Y(3), X(0)];

        return
        [
            .. Start(
                1,
                2,
                1,
                0
            ),
            Allocate, U(4), U(0), InitYRegisters, ExtendedListOperand, U(3), Y(1), Y(2), Y(3),
            TryEnter, Y(0), F(Handler), ExternalCall, U(0), U(0),
            .. spill, .. checks, Jump, F(Success),
            Label, U(Failure), .. restore, Line, U(1), BadMatch, X(0),
            Label, U(Success), .. outgoing, Line, U(2), ExternalCall, U(tuple ? 2 : 1), U(1),
            .. live, TryEnd, Y(0), Deallocate, U(4), Return,
            Label, U(Handler), TryCase, Y(0), Move, A(1), X(0), Deallocate, U(4), Return, EndCode
        ];
    }

    private static byte[]? Rewrite(BeamModuleCompiler compiler, ReadOnlyMemory<byte> code) =>
        (byte[]?)Call(
            compiler,
            "KeepCallResultThroughCheckedProjection",
            code,
            6
        );
}
