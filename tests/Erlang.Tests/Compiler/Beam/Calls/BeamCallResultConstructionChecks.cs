using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamCallResultConstructionChecks
{
    internal static Task Run()
    {
        const string source = """
            -module(call_construction).
            -export([forward/0, dependent/0, expose/1, pack/3]).
            forward() ->
                Left = ?MODULE:expose(left),
                Right = ?MODULE:expose(right),
                Work = ?MODULE:pack(worker, [{self(), []}], [server]),
                {Left, Right, Work}.
            dependent() ->
                Left = ?MODULE:expose(left),
                Right = ?MODULE:expose(right),
                Work = ?MODULE:pack(worker, [{self(), Right}], [server]),
                {Left, Right, Work}.
            expose(Value) -> Value.
            pack(N, A, S) -> {N, A, S}.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "call_construction.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> independent =
        [
            BeamEmissionFormat.ExternalCall, 0x10, 0x00, BeamEmissionFormat.TestHeap, 0x50, 0x10,
            BeamEmissionFormat.BifZero, 0x10, 0x13,
            BeamEmissionFormat.PutTuple, 0x13, 0x17, 0x20, 0x13, 0x02,
            BeamEmissionFormat.PutList, 0x13, 0x02, 0x13, BeamEmissionFormat.Move, 0x03, 0x14
        ];
        ReadOnlySpan<byte> dependent =
        [
            BeamEmissionFormat.ExternalCall, 0x10, 0x00, BeamEmissionFormat.Move, 0x03, 0x14,
            BeamEmissionFormat.TestHeap, 0x50, 0x00, BeamEmissionFormat.BifZero, 0x10, 0x03,
            BeamEmissionFormat.PutTuple, 0x03, 0x17, 0x20, 0x03, 0x14,
            BeamEmissionFormat.PutList, 0x03, 0x02, 0x13
        ];
        if (code.AsSpan().IndexOf(independent) < 0 || code.AsSpan().IndexOf(dependent) < 0)
        {
            throw new InvalidOperationException(BeamCallResultConstructionDiagnostics.StoreOrderMismatch);
        }

        _ = BeamModule.Prepare(beam);

        return Task.CompletedTask;
    }
}
