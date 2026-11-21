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
        VerifyNullaryCallArguments();

        return Task.CompletedTask;
    }

    private static void VerifyNullaryCallArguments()
    {
        const string source = """
            -module(call_arguments).
            -export([node_arg/1, self_arg/1, effect/1]).
            node_arg(X) ->
                Result = term_to_binary(X),
                [Result, atom_to_list(node())].
            self_arg(X) ->
                Result = term_to_binary(X),
                {Result, pid_to_list(self())}.
            effect(X) ->
                Result = term_to_binary(X),
                {Result, erase()}.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "call_arguments.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> nodeArgument =
        [
            BeamEmissionFormat.ExternalCall, 0x10, 0x00,
            BeamEmissionFormat.BifZero, 0x10, 0x13,
            BeamEmissionFormat.Move, 0x03, 0x04,
            BeamEmissionFormat.Move, 0x13, 0x03
        ];
        ReadOnlySpan<byte> selfArgument =
        [
            BeamEmissionFormat.ExternalCall, 0x10, 0x00,
            BeamEmissionFormat.BifZero, 0x30, 0x13,
            BeamEmissionFormat.Move, 0x03, 0x04,
            BeamEmissionFormat.Move, 0x13, 0x03
        ];
        ReadOnlySpan<byte> effectBeforeLine =
        [
            BeamEmissionFormat.ExternalCall, 0x10, 0x00,
            BeamEmissionFormat.Move, 0x03, 0x04,
            BeamEmissionFormat.Line
        ];
        ReadOnlySpan<byte> effectCall = [BeamEmissionFormat.ExternalCall, 0x00, 0x50];
        if (code.AsSpan().IndexOf(nodeArgument) < 0 || code.AsSpan().IndexOf(selfArgument) < 0
            || code.AsSpan().IndexOf(effectBeforeLine) < 0 || code.AsSpan().IndexOf(effectCall) < 0)
        {
            throw new InvalidOperationException(BeamCallResultConstructionDiagnostics.NullaryArgumentOrderMismatch);
        }

        _ = BeamModule.Prepare(beam);
    }
}
