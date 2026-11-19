using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamMapCallContinuationChecks
{
    internal static Task Run()
    {
        const string source = """
            -module(map_call_continuation).
            -export([run/3, keep/3, dispatch/2]).
            run(Module, Server, Argument) ->
                Spec = #{id => make_ref(), start => {Module, start_link, [Argument]}, modules => [Module]},
                case ?MODULE:dispatch(Server, Spec) of
                    {ok, Value} -> {ok, Value};
                    false -> missing
                end.
            keep(Module, Server, Argument) ->
                Spec = #{id => make_ref(), start => {Module, start_link, [Argument]}, modules => [Module]},
                case ?MODULE:dispatch(Server, Spec) of
                    {ok, Value} -> {ok, Value};
                    false -> {missing, Spec}
                end.
            dispatch(pass, Spec) -> {ok, Spec};
            dispatch(_, _) -> false.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "map_call_continuation.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> rootedProducer =
        [
            BeamEmissionFormat.ExternalCall, 0x00, 0x00, BeamEmissionFormat.TestHeap, 0x80, 0x10,
            BeamEmissionFormat.PutList, 0x04, 0x02, 0x13
        ];
        ReadOnlySpan<byte> mapArgument =
        [
            BeamEmissionFormat.PutMapAssoc, 0x05, 0x47, 0x00, 0x13, 0x30, 0x17, 0x60
        ];
        ReadOnlySpan<byte> retainedMap =
        [
            BeamEmissionFormat.ExternalCall, 0x00, 0x00, BeamEmissionFormat.Move, 0x03, 0x04,
            BeamEmissionFormat.TestHeap, 0x80, 0x00, BeamEmissionFormat.PutList, 0x14, 0x02, 0x03
        ];
        int offset = code.AsSpan().IndexOf(rootedProducer);
        if (offset < 0 || code.AsSpan(offset).IndexOf(mapArgument) < 0
            || code.AsSpan(offset + rootedProducer.Length).IndexOf(retainedMap) < 0)
        {
            throw new InvalidOperationException(BeamMapCallContinuationDiagnostics.MapRootsMismatch);
        }

        _ = BeamModule.Prepare(beam);

        return Task.CompletedTask;
    }
}
