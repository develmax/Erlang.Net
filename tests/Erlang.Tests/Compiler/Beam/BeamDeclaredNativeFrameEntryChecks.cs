using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamDeclaredNativeFrameEntryChecks
{
    internal static Task Run()
    {
        const string source = """
            -module(beam_declared_nif_frame_probe).
            -export([marked/1, simple/1, ordinary/1]).
            -nifs([marked/1, simple/1]).
            marked(Value) ->
                Current = external_probe:producer(),
                {Current, Value}.
            simple(Value) -> erlang:nif_error(Value).
            ordinary(Value) ->
                Current = external_probe:producer(),
                {Current, Value}.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "beam_declared_nif_frame_probe.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> setup =
        [
            BeamEmissionFormat.Allocate, 0x10, 0x10, BeamEmissionFormat.Move, 0x03, 0x04
        ];
        int marked = code.AsSpan().IndexOf(setup);
        int simple = marked < 0 ? -1 : code.AsSpan(marked + setup.Length + 1).IndexOf(BeamEmissionFormat.NifStart);
        int simpleMarker = simple < 0 ? -1 : marked + setup.Length + 1 + simple;
        int ordinary = simpleMarker < 0 ? -1 : code.AsSpan(simpleMarker + 1).IndexOf(setup);
        if (marked < 0 || code[marked + setup.Length] != BeamEmissionFormat.NifStart
            || simpleMarker < 0 || code[simpleMarker + 1] != BeamEmissionFormat.Line || ordinary < 0
            || code[simpleMarker + 1 + ordinary + setup.Length] != BeamEmissionFormat.Line)
        {
            throw new InvalidOperationException("The declared native marker must follow only its own entry frame setup.");
        }

        return Task.CompletedTask;
    }
}
