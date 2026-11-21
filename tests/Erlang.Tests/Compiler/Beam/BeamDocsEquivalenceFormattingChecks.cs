using System.Text;
using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamDocsEquivalenceFormattingChecks
{
    internal const string TestName = "compiler/beam/documentation-equivalence-retains-empty-strings-and-wraps-arguments";
    private const string Fixture = "beam_docs_equiv_format.erl";
    private const string Mismatch = "Equivalence documentation did not follow the OTP expression printer.";
    private const string Source = """
        -module(beam_docs_equiv_format).
        -export([empty/1, nested/1, options/1, flat/1, mixed/1]).
        -doc #{equiv => decode(Entry, "", [])}.
        empty(Entry) -> Entry.
        -doc #{equiv => call_with_context(Primary, Secondary, Tertiary, resolver:choose(Primary, current))}.
        nested(Input) -> Input.
        -doc #{equiv => call_with_options(Parent, Module, Function, Args, Pattern, [{set_on_spawn, true}])}.
        options(Input) -> Input.
        -doc #{equiv => small(A, {ok, B}, C)}.
        flat(Input) -> Input.
        -doc #{equiv => call_with_options(FirstArgument, {ok, LongArgument}, NextArgument, LastArgument)}.
        mixed(Input) -> Input.
        """;

    internal static Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] docs = Chunks(beam).Single(chunk => chunk.Id == "Docs").Data;
        if (ExternalTermFormat.Decode(RawEtf(docs)) is not TupleTerm { Items: [_, _, _, _, _, _, var entries] })
        {
            throw new InvalidOperationException(Mismatch);
        }

        string[] expected =
        [
            "call_with_options(FirstArgument,\n                  {ok, LongArgument},\n                  NextArgument, LastArgument)",
            "small(A, {ok, B}, C)",
            "call_with_options(Parent, Module, Function, Args, Pattern,\n                  [{set_on_spawn, true}])",
            "call_with_context(Primary, Secondary, Tertiary,\n                  resolver:choose(Primary, current))",
            """decode(Entry, "", [])"""
        ];
        var actual = Cons.Items(entries).Cast<TupleTerm>()
            .Select(entry => ((MapTerm)entry.Items[4]).Entries.Single(pair => pair.Key.Equals(Term.A("equiv"))).Value);
        if (!actual.SequenceEqual(expected.Select(value => (Term)new BitString(Encoding.UTF8.GetBytes(value)))))
        {
            throw new InvalidOperationException(Mismatch);
        }

        return Task.CompletedTask;
    }
}
