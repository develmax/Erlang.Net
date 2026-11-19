using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamInPlaceTupleHeapChecks
{
    internal const string TestName = "compiler/beam/in-place-tuple-projection-keeps-heap-roots";

    internal static Task Run()
    {
        const string source = """
            -module(tuple_heap_roots).
            -export([root_one/1, root_two/2, call_barrier/2, result/1]).
            root_one(Input) ->
                case ?MODULE:result(Input) of
                    {source, Value} -> {target, Value};
                    Other -> Other
                end.
            root_two(Name, {problem, Reason}) ->
                {problem, {file, Name, Reason}}.
            call_barrier(Name, {problem, Reason}) ->
                Value = ?MODULE:result(Reason),
                {problem, {file, Name, Value}}.
            result(Value) -> Value.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "tuple_heap_roots.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> oneRoot =
        [
            BeamEmissionFormat.TestHeap, 0x30, 0x10,
            BeamEmissionFormat.GetTupleElement, 0x03, 0x10, 0x03, BeamEmissionFormat.PutTuple
        ];
        ReadOnlySpan<byte> twoRoots =
        [
            BeamEmissionFormat.TestHeap, 0x70, 0x20,
            BeamEmissionFormat.GetTupleElement, 0x13, 0x10, 0x13, BeamEmissionFormat.PutTuple
        ];
        ReadOnlySpan<byte> callBarrier =
        [
            BeamEmissionFormat.GetTupleElement, 0x13, 0x10, 0x03,
            BeamEmissionFormat.Line, 0x50, BeamEmissionFormat.ExternalCall, 0x10, 0x00,
            BeamEmissionFormat.TestHeap, 0x70, 0x10
        ];
        if (code.AsSpan().IndexOf(oneRoot) < 0
            || code.AsSpan().IndexOf(twoRoots) < 0
            || code.AsSpan().IndexOf(callBarrier) < 0)
        {
            throw new InvalidOperationException(BeamInPlaceTupleHeapDiagnostics.HeapOrderMismatch);
        }

        _ = BeamModule.Prepare(beam);

        return Task.CompletedTask;
    }
}
