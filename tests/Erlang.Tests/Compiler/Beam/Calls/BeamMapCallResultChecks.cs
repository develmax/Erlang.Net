using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamMapCallResultChecks
{
    internal const string TestName = "compiler/beam/map-allocation-keeps-call-result-root";

    internal static Task Run()
    {
        const string source = """
            -module(map_root_result).
            -export([direct/1, retained/1, dynamic_key/1, extend/1]).
            direct(Value) ->
                Term = probe:value(Value),
                #{payload => Term, tag => literal}.
            retained(Value) ->
                Term = probe:value(Value),
                Map = #{payload => Term, tag => literal},
                {Map, Term}.
            dynamic_key(Value) ->
                Key = probe:key(Value),
                #{Key => present}.
            extend(Value) ->
                M = base(Value),
                M#{tag => added}.
            base(Value) ->
                probe:effect(),
                #{payload => Value}.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "map_root_result.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> directValue =
        [
            BeamEmissionFormat.Allocate, 0x00, 0x10, BeamEmissionFormat.Line, 0x20,
            BeamEmissionFormat.ExternalCall, 0x10, 0x00,
            BeamEmissionFormat.PutMapAssoc, 0x05, 0x47, 0x00, 0x03, 0x10,
            BeamEmissionFormat.ExtendedListOperand, 0x40, 0x52, 0x03, 0x62, 0x72,
            BeamEmissionFormat.Deallocate, 0x00
        ];
        ReadOnlySpan<byte> reusedValue =
        [
            BeamEmissionFormat.ExternalCall, 0x10, 0x00, BeamEmissionFormat.Move, 0x03, 0x04,
            BeamEmissionFormat.PutMapAssoc, 0x05, 0x47, 0x00, 0x03, 0x00,
            BeamEmissionFormat.ExtendedListOperand, 0x40, 0x52, 0x04, 0x62, 0x72
        ];
        ReadOnlySpan<byte> directKey =
        [
            BeamEmissionFormat.ExternalCall, 0x10, 0x10,
            BeamEmissionFormat.PutMapAssoc, 0x05, 0x47, 0x00, 0x03, 0x10,
            BeamEmissionFormat.ExtendedListOperand, 0x20, 0x03, 0xB2, BeamEmissionFormat.Deallocate, 0x00
        ];
        if (code.AsSpan().IndexOf(directValue) < 0 || code.AsSpan().IndexOf(reusedValue) < 0
            || code.AsSpan().IndexOf(directKey) < 0)
        {
            throw new InvalidOperationException(BeamMapCallResultDiagnostics.ResultRootMismatch);
        }

        _ = BeamModule.Prepare(beam);

        return Task.CompletedTask;
    }
}
