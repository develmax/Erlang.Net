using System.Text;
using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamDiameterAccessorDocsChecks
{
    internal const string TestName = "compiler/beam/diameter-documentation-includes-generated-accessors";
    private const string Fixture = "beam_diameter_accessor_docs.erl";
    private const string MappedFile = "mapped_diameter.hrl";
    private const string Transform = "-compile({parse_transform, diameter_exprecs}).";
    private const string HiddenDocs = "-moduledoc false.";
    private const string Mismatch = "The generated Diameter accessors did not have the expected documentation.";
    private const string Source = """
        -module(beam_diameter_accessor_docs).
        -moduledoc false.
        -compile({parse_transform, diameter_exprecs}).
        -record(sample, {key = 0, value = undefined}).
        -record(other, {value = none}).
        -record(unexported, {unexported = none}).
        -export_records([sample, other]).
        -export([value/0]).
        -file("mapped_diameter.hrl", 50).
        value() -> #unexported{}.
        """;

    internal static Task Run()
    {
        var entries = ReadEntries(Source);
        (string Name, int Arity)[] expected =
        [
            ("value", 0),
            ("#info-other", 1), ("#set-other", 2), ("#get-other", 2), ("#get-other", 1),
            ("#new-other", 1), ("#new-other", 0),
            ("#info-sample", 1), ("#set-sample", 2), ("#get-sample", 2), ("#get-sample", 1),
            ("#new-sample", 1), ("#new-sample", 0),
            ("#set-", 2), ("#get-", 2), ("#get-", 1), ("#new-", 2), ("#new-", 1), ("#info-", 2), ("#info-", 1)
        ];
        if (!entries.Select(entry => entry.Items[0]).SequenceEqual(expected.Select(item =>
            (Term)Term.Tuple(Term.A("function"), Term.A(item.Name), Term.I(item.Arity)))))
        {
            throw new InvalidOperationException(Mismatch);
        }

        VerifyEntry(entries[1], 301, "'#info-other'/1");
        VerifyEntry(entries[4], 281, "'#get-other'(Rec)");
        VerifyEntry(entries[6], 252, "'#new-other'()");
        VerifyEntry(entries[^1], 188, "'#info-'(RecName)");

        var plainEntries = ReadEntries(Source.Replace(Transform, string.Empty, StringComparison.Ordinal));
        byte[] noDocs = CodeGeneration.CompileBeamModule(Source.Replace(HiddenDocs, string.Empty, StringComparison.Ordinal), Fixture);
        if (plainEntries.Length != 1 || !plainEntries[0].Equals(entries[0])
            || Chunks(noDocs).Any(chunk => chunk.Id == "Docs"))
        {
            throw new InvalidOperationException(Mismatch);
        }

        return Task.CompletedTask;
    }

    private static TupleTerm[] ReadEntries(string source)
    {
        byte[] docs = Chunks(CodeGeneration.CompileBeamModule(source, Fixture)).Single(chunk => chunk.Id == "Docs").Data;
        if (ExternalTermFormat.Decode(RawEtf(docs)) is not TupleTerm { Items: [_, _, _, _, _, _, var entries] })
        {
            throw new InvalidOperationException(Mismatch);
        }

        return Cons.Items(entries).Cast<TupleTerm>().ToArray();
    }

    private static void VerifyEntry(TupleTerm entry, int line, string signature)
    {
        Term position = Term.I(line);
        Term annotation = Term.List(
            Term.Tuple(Term.A("file"), Term.String(MappedFile)),
            Term.Tuple(Term.A("location"), position)
        );
        Term metadata = new MapTerm(new Dictionary<Term, Term> { [Term.A("source_anno")] = position });
        if (!entry.Items[1].Equals(annotation)
            || !entry.Items[2].Equals(Term.List(new BitString(Encoding.UTF8.GetBytes(signature))))
            || !entry.Items[3].Equals(Term.A("none"))
            || !entry.Items[4].Equals(metadata))
        {
            throw new InvalidOperationException(Mismatch);
        }
    }
}
