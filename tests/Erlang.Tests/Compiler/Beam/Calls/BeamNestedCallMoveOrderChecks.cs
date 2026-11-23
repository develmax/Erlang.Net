using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamNestedCallMoveOrderChecks
{
    private const string SavedLoadMismatch = "The independent stack load did not precede the X0 result copy.";
    private const string ResultCopyMismatch = "The result in X0 was not copied before replacing the first argument.";

    internal static void Run()
    {
        const string source = """
            -module(beam_nested_call_move_order).
            -export([positive/1, different/1, retained/1]).
            positive(V) -> consume(tag, V, erlang:loaded()).
            different(V) -> consume(tag, erlang:loaded(), V).
            retained(V) -> consume(erlang:loaded(), V, tag).
            consume(A, B, C) -> {A, B, C}.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "beam_nested_call_move_order.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;

        ReadOnlySpan<byte> savedBeforeResult =
        [
            BeamEmissionFormat.Move, 0x04, 0x13,
            BeamEmissionFormat.Move, 0x03, 0x23,
            BeamEmissionFormat.Move
        ];
        if (code.AsSpan().IndexOf(savedBeforeResult) < 0)
        {
            throw new InvalidOperationException(SavedLoadMismatch);
        }

        ReadOnlySpan<byte> resultInSecondArgument = [BeamEmissionFormat.Move, 0x03, 0x13, BeamEmissionFormat.Move];
        if (code.AsSpan().IndexOf(resultInSecondArgument) < 0)
        {
            throw new InvalidOperationException(ResultCopyMismatch);
        }

        _ = BeamModule.Prepare(beam);
    }
}
