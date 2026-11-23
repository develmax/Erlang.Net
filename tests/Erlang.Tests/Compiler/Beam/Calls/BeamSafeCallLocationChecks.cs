using System.Buffers.Binary;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamSafeCallLocationChecks
{
    internal static Task Run()
    {
        const string source = """
            -module(safe_call_location).
            -export([forward/1, keep/1, accept/1, throwing/1]).
            forward(Observer) ->
                Ref = erlang:make_ref(),
                Args = [{ref, Ref}, Observer],
                Spec = #{ref => Ref, args => Args},
                ?MODULE:accept(Spec).
            keep(Value) ->
                Ref = erlang:make_ref(), Result = ?MODULE:throwing(Value),
                {Ref, Result}.
            accept(Spec) -> Spec.
            throwing(Value) -> erlang:element(2, Value).
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "safe_call_location.erl");
        var chunks = BeamDocumentationEmissionChecks.Chunks(beam).ToDictionary(chunk => chunk.Id, chunk => chunk.Data);
        byte[] code = chunks["Code"];
        byte[] lines = chunks["Line"];
        ReadOnlySpan<byte> independent =
        [
            BeamEmissionFormat.Move, 0x03, 0x04, BeamEmissionFormat.ExternalCall, 0x00, 0x00,
            BeamEmissionFormat.Move, 0x03, 0x14,
            BeamEmissionFormat.TestHeap, 0x70, 0x00
        ];
        ReadOnlySpan<byte> shared =
        [
            BeamEmissionFormat.ExternalCall, 0x00, 0x00,
            BeamEmissionFormat.Move, 0x04, 0x13, BeamEmissionFormat.Move, 0x03, 0x04,
            BeamEmissionFormat.Move, 0x13, 0x03,
            BeamEmissionFormat.Line, 0x40, BeamEmissionFormat.ExternalCall, 0x10, 0x20
        ];
        if (code.AsSpan().IndexOf(independent) < 0
            || code.AsSpan().IndexOf(shared) < 0
            || BinaryPrimitives.ReadUInt32BigEndian(lines.AsSpan(8, sizeof(uint))) != 8
            || BinaryPrimitives.ReadUInt32BigEndian(lines.AsSpan(12, sizeof(uint))) != 6)
        {
            throw new InvalidOperationException(BeamSafeCallLocationDiagnostics.LineMismatch);
        }

        _ = BeamModule.Prepare(beam);

        return Task.CompletedTask;
    }
}
