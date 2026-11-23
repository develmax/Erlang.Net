using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamDocsRecordTypeChecks
{
    internal const string TestName = "compiler/beam/documentation-follows-typed-record-fields";
    private const string Fixture = "beam_docs_record_fields.erl";
    private const string Mismatch = "Documentation type entries did not follow record defaults and explicit field types.";
    private const string Source = """
        -module(beam_docs_record_fields).
        -moduledoc false.
        -export([node/0, replace/1]).
        -type leaf() :: pos_integer().
        -type replaced() :: binary().
        -type chosen() :: atom().
        -type unrelated() :: integer().
        -record(node, {value :: leaf(), next :: node_type() | done}).
        -record(replace, {payload :: replaced()}).
        -record(unused, {payload :: unrelated()}).
        -type node_type() :: #node{}.

        -spec node() -> node_type().
        node() -> #node{value = 1, next = done}.

        -spec replace(#replace{payload :: chosen()}) -> ok.
        replace(#replace{}) -> ok.
        """;

    internal static Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] docs = Chunks(image).Single(chunk => chunk.Id == "Docs").Data;
        if (ExternalTermFormat.Decode(RawEtf(docs)) is not TupleTerm { Items: [_, _, _, _, _, _, var entries] })
        {
            throw new InvalidOperationException(Mismatch);
        }

        var actual = Cons.Items(entries).OfType<TupleTerm>()
            .Select(entry => entry.Items[0])
            .OfType<TupleTerm>()
            .Where(key => key.Items is [Atom { Name: "type" }, Atom, _])
            .Select(key => ((Atom)key.Items[1]).Name);
        string[] expected = ["node_type", "chosen", "leaf"];
        if (!actual.SequenceEqual(expected))
        {
            throw new InvalidOperationException(Mismatch);
        }

        return Task.CompletedTask;
    }
}
