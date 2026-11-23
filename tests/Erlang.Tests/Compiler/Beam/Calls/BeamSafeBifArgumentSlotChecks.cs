using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamSafeBifArgumentSlotChecks
{
    internal const string TestName = "compiler/beam/reuses-a-result-slot-through-a-safe-zero-arity-bif";
    private const string Failure = "The closed result frame may shrink only across a proven nonthrowing BIF.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_safe_bif_argument_slot");
        byte get = Import("get_cookie", 0);
        byte node = Import("node", 0);
        byte unknown = Import("other", 0);
        byte set = Import("set_cookie", 2);
        byte[] start = Start(
            1,
            2,
            1,
            1
        );
        byte[] suffix = [Line, U(1), ExternalCall, U(2), set];
        byte[] original = Original(node);
        byte[] expected =
        [
            .. start, Allocate, U(1), U(1), Move, X(0), Y(0), ExternalCall, U(0), get, BifZero, node, X(1),
            Move, Y(0), X(2), Move, X(0), Y(0), Move, X(1), X(0), Move, X(2), X(1),
            .. suffix, Move, Y(0), X(0), Deallocate, U(1), Return, EndCode
        ];
        if (Rewrite(original) is not { } actual || !actual.SequenceEqual(expected)
            || Rewrite(expected) is not null || Rewrite(Original(unknown)) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;

        byte Import(string function, int arity) => U((int)Call(
            compiler,
            "Import",
            "erlang",
            function,
            arity
        ));

        byte[] Original(byte bif) =>
        [
            .. start, Allocate, U(2), U(1), InitYRegisters, ExtendedListOperand, U(1), Y(1),
            Move, X(0), Y(0), ExternalCall, U(0), get, Move, X(0), Y(1), BifZero, bif, X(0),
            Move, Y(0), X(1), .. suffix, Move, Y(1), X(0), Deallocate, U(2), Return, EndCode
        ];

        byte[]? Rewrite(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "ReuseCallResultSlotForSafeBifArgument",
            code,
            3
        );
    }
}
