using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamTransientFrameChecks
{
    private const string ProjectionMismatch = "A call-result projection occupied a stack slot that did not cross another call.";
    private const string SavedResultMismatch = "A projection that must survive a collecting call lost its stack slot.";

    internal static void Run()
    {
        const string source = """
            -module(beam_transient_frame).
            -export([value/1,keep/1]).
            value(Value) ->
                [Item] = decode({3,3}, Value),
                Item.
            keep(Value) ->
                [Item] = decode({3,3}, Value),
                erlang:garbage_collect(),
                Item.
            decode(_, Value) when is_list(Value) -> Value;
            decode(_, _) -> bad.
            """;
        byte[] bytes = CodeGeneration.CompileBeamModule(source, "beam_transient_frame.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(bytes).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> emptyFrame = [BeamEmissionFormat.Allocate, 0x00, 0x10];
        ReadOnlySpan<byte> registerProjection = [BeamEmissionFormat.GetList, 0x03, 0x13, 0x23];
        if (code.AsSpan().IndexOf(emptyFrame) < 0 || code.AsSpan().IndexOf(registerProjection) < 0)
        {
            throw new InvalidOperationException(ProjectionMismatch);
        }

        ReadOnlySpan<byte> returnSavedFirst = [BeamEmissionFormat.Move, 0x04, 0x03, BeamEmissionFormat.Deallocate];
        ReadOnlySpan<byte> returnSavedSecond = [BeamEmissionFormat.Move, 0x14, 0x03, BeamEmissionFormat.Deallocate];
        if (code.AsSpan().IndexOf(returnSavedFirst) < 0 && code.AsSpan().IndexOf(returnSavedSecond) < 0)
        {
            throw new InvalidOperationException(SavedResultMismatch);
        }

        _ = BeamModule.Prepare(bytes);
    }
}
