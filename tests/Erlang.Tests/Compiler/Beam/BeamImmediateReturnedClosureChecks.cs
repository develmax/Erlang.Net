using System.Buffers.Binary;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamImmediateReturnedClosureChecks
{
    internal const string TestName = "compiler/beam/keeps-outer-identity-for-an-immediate-tail-application";
    private const string SourceFile = "beam_immediate_returned_closure.erl";
    private const string Construction = "Expected one escaping closure with its original outer identity.";
    private const string Source = """
        -module(beam_immediate_returned_closure).
        %% options
        -export([make/0]).
        make() ->
            fun() ->
                begin (fun() -> erlang:get(key) end)() end
            end.
        """;

    internal static Task Run()
    {
        byte[] inline = CodeGeneration.CompileBeamModule(Source, SourceFile);
        byte[] disabled = BeamAdjacentNumericResultChecks.Compile(
            Source.Replace("%% options", "-compile(no_ssa_opt).", StringComparison.Ordinal),
            disabled: true
        );
        if (Functions(inline) != 1 || Functions(disabled) != 2
            || BeamDocumentationEmissionChecks.Chunks(inline).Single(chunk => chunk.Id == "AtU8")
                .Data.AsSpan().IndexOf("-make/0-fun-1-"u8) < 0)
        {
            throw new InvalidOperationException(Construction);
        }

        return Task.CompletedTask;
    }

    private static uint Functions(byte[] image) => BinaryPrimitives.ReadUInt32BigEndian(
        BeamDocumentationEmissionChecks.Chunks(image).Single(chunk => chunk.Id == "FunT").Data
    );
}
