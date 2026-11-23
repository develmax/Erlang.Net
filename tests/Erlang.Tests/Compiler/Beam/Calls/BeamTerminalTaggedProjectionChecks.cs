using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamTerminalTaggedProjectionChecks
{
    internal static Task Run()
    {
        const string source = """
            -module(terminal_tuple_selection).
            -export([project/3, checked/3, choose/2]).
            project(Kind, Reply, Argument) ->
                Request = #{id => make_ref(), payload => {Kind, [Argument]}},
                case ?MODULE:choose(Reply, Request) of
                    {good, Value} -> {good, Value};
                    {good, Value, _Info, _Context} -> {good, Value};
                    {bad, {Reason, _Info}} -> {bad, Reason};
                    {bad, Reason} -> {bad, Reason}
                end.
            checked(Kind, Reply, Argument) ->
                Request = #{id => make_ref(), payload => {Kind, [Argument]}},
                case ?MODULE:choose(Reply, Request) of
                    {good, Value} -> {good, Value};
                    {good, Value, _Info, _Context} -> {good, Value};
                    {bad, {Reason, fixed}} -> {bad, Reason};
                    {bad, Reason} -> {bad, Reason}
                end.
            choose(Reply, #{id := _Id}) -> Reply.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "terminal_tuple_selection.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> direct =
        [
            BeamEmissionFormat.TestHeap, 0x30, 0x10, BeamEmissionFormat.GetTupleElement, 0x03, 0x10, 0x03,
            BeamEmissionFormat.PutTuple
        ];
        ReadOnlySpan<byte> nested =
        [
            BeamEmissionFormat.TestHeap, 0x30, 0x20, BeamEmissionFormat.GetTupleElement, 0x13, 0x00, 0x03,
            BeamEmissionFormat.PutTuple
        ];
        int first = code.AsSpan().IndexOf(direct);
        if (first < 0 || code.AsSpan(first + direct.Length).IndexOf(nested) < 0)
        {
            throw new InvalidOperationException(BeamTerminalTaggedProjectionDiagnostics.LivePrefixMismatch);
        }

        _ = BeamModule.Prepare(beam);

        return Task.CompletedTask;
    }
}
