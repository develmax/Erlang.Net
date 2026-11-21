using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamQuotedFeatureAtomChecks
{
    internal const string TestName = "compiler/beam/preserve-quoted-feature-keyword-type-annotations";
    private const string Module = "beam_quoted_feature_atoms";
    private const string Header = "quoted_feature_atoms.hrl";

    internal static Task Run()
    {
        var root = Directory.CreateTempSubdirectory("erlang-feature-quoted-");
        try
        {
            Verify(root.FullName, "");
            Verify(root.FullName, "-feature(maybe_expr, enable).");
            Verify(root.FullName, "-feature(maybe_expr, disable).");
        }
        finally
        {
            root.Delete(recursive: true);
        }

        return Task.CompletedTask;
    }

    internal static byte[] Verify(string directory, string feature)
    {
        string source = Path.Combine(directory, Module + ".erl");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, Header), "-type from_header() :: {'else', 'ordinary'}.\n");
        string unquoted = feature.Contains("disable", StringComparison.Ordinal) ? ", maybe, else" : "";
        string prefix = feature.Length == 0 ? feature : feature + "\n";
        File.WriteAllText(
            source,
            prefix + $$"""
            -module({{Module}}).
            -export([value/0]).
            -export_type([sample/0, expanded/0, passed/0, from_header/0]).
            -include("{{Header}}").
            -define(REPLACEMENT, 'maybe').
            -define(IDENTITY(X), X).
            -type sample() :: {'maybe', 'else', 'ordinary', '\x{6D}aybe', 'maybex', 'case'{{unquoted}}}.
            -type expanded() :: ?REPLACEMENT.
            -type passed() :: ?IDENTITY('else').
            -record(sample_record, {'else' = none :: 'maybe' | 'ordinary'}).
            -spec value() -> {'maybe', 'else', 'ordinary'}.
            value() -> {'maybe', 'else', 'ordinary'}.

            """
        );
        var profile = new BeamReleaseBuildProfile(
            "10.0.6",
            "/release/src/" + Module + ".erl",
            "/release/src",
            [],
            []
        );
        byte[] beam = CodeGeneration.CompileBeamFile(source, buildProfile: profile);
        byte[] debug = Chunks(beam).Single(chunk => chunk.Id == "Dbgi").Data;
        if (ExternalTermFormat.Decode(RawEtf(debug)) is not TupleTerm
            {
                Items: [_, _, TupleTerm { Items: [var forms, _] }]
            })
        {
            throw new InvalidOperationException("Invalid debug information for quoted feature atom fixture.");
        }

        var types = Cons.Items(forms).OfType<TupleTerm>()
            .Where(form => form.Items is [Atom { Name: "attribute" }, _, Atom { Name: "type" }, TupleTerm])
            .Select(form => (TupleTerm)form.Items[3])
            .ToDictionary(form => ((Atom)form.Items[0]).Name, form => form.Items[1]);
        string?[] sample = unquoted.Length == 0
            ? ["'maybe'", "'else'", null, null, null, null]
            : ["'maybe'", "'else'", null, null, null, null, null, null];
        CheckText(types["sample"], sample);
        CheckText(types["expanded"], [null]);
        CheckText(types["passed"], ["'else'"]);
        CheckText(types["from_header"], ["'else'", null]);
        var record = Cons.Items(forms).OfType<TupleTerm>().Single(form =>
            form.Items is [Atom { Name: "attribute" }, _, Atom { Name: "record" }, _]);
        if (record.Items[3] is not TupleTerm { Items: [_, var fields] }
            || Cons.Items(fields).SingleOrDefault() is not TupleTerm
            {
                Items:
                [
                    Atom { Name: "typed_record_field" },
                    TupleTerm { Items: [Atom { Name: "record_field" }, var location, TupleTerm atom, _] },
                    _
                ]
            }
            || QuotedText(atom) != "'else'" || !location.Equals(atom.Items[1]))
        {
            throw new InvalidOperationException("A typed record field lost the scanner's quoted keyword annotation.");
        }

        return beam;
    }

    private static void CheckText(Term type, IReadOnlyList<string?> expected)
    {
        var nodes = type is TupleTerm { Items: [Atom { Name: "type" }, _, Atom { Name: "tuple" }, var members] }
            ? Cons.Items(members).ToArray()
            : [type];
        string?[] actual = nodes.Select(QuotedText).ToArray();
        if (!actual.SequenceEqual(expected))
        {
            throw new InvalidOperationException("Quoted feature keyword annotations differ from the OTP scanner policy.");
        }
    }

    private static string? QuotedText(Term atom)
    {
        if (atom is not TupleTerm { Items: [Atom { Name: "atom" }, var annotation, _] })
        {
            throw new InvalidOperationException("Expected an atom in the quoted feature keyword type.");
        }

        return annotation is Cons list && Cons.Items(list).FirstOrDefault() is TupleTerm
        {
            Items: [Atom { Name: "text" }, var spelling]
        }
            ? string.Concat(Cons.Items(spelling).Select(character => char.ConvertFromUtf32((int)((Integer)character).Value)))
            : null;
    }
}
