using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamReleaseSearchIncludeChecks
{
    internal const string TestName = "compiler/beam/release-debug-records-selected-include-directory";
    private const string Module = "beam_search_include";
    private const string FileName = Module + ".erl";
    private const string BuildInclude = "/release/src/../include";
    private const string Failure = "Release debug metadata lost the selected include directory or the parent header path.";
    private const string Source = """
        -module(beam_search_include).
        -include("shadow.hrl").
        -include("unmapped.hrl").
        -include("nested/first.hrl").
        -export([value/0]).
        value() -> ok.
        """;

    internal static Task Run()
    {
        var temporary = Directory.CreateTempSubdirectory("erlang-beam-search-include-");
        try
        {
            string directory = temporary.FullName;
            string unmapped = Directory.CreateDirectory(Path.Combine(directory, "unmapped")).FullName;
            string recorded = Directory.CreateDirectory(Path.Combine(directory, "recorded")).FullName;
            string nested = Directory.CreateDirectory(Path.Combine(recorded, "nested")).FullName;
            string source = Path.Combine(directory, FileName);
            File.WriteAllText(source, Source);
            File.WriteAllText(Path.Combine(directory, "shadow.hrl"), "-record(shadow, {value}).\n");
            File.WriteAllText(Path.Combine(recorded, "shadow.hrl"), "-error(unexpected_include).\n");
            File.WriteAllText(Path.Combine(unmapped, "unmapped.hrl"), "-record(unmapped, {value}).\n");
            File.WriteAllText(Path.Combine(nested, "first.hrl"), "-include(\"sibling.hrl\").\n-record(first, {value}).\n");
            File.WriteAllText(Path.Combine(nested, "sibling.hrl"), "-record(sibling, {value}).\n");
            var profile = new BeamReleaseBuildProfile(
                "10.0.6",
                "/release/src/" + FileName,
                "/release/src",
                [BuildInclude],
                []
            )
            {
                RecordedIncludeSearchDirectories = new Dictionary<int, string> { [1] = BuildInclude }
            };
            byte[] image = CodeGeneration.CompileBeamFile(source, includeDirectories: [unmapped, recorded], buildProfile: profile);
            byte[] debug = Chunks(image).Single(chunk => chunk.Id == "Dbgi").Data;
            Term decoded = ExternalTermFormat.Decode(RawEtf(debug));
            Term[] expected =
            [
                Term.String(FileName),
                Term.String("shadow.hrl"),
                Term.String(FileName),
                Term.String("unmapped.hrl"),
                Term.String(FileName),
                Term.String(BuildInclude + "/nested/first.hrl"),
                Term.String(BuildInclude + "/nested/sibling.hrl"),
                Term.String(BuildInclude + "/nested/first.hrl"),
                Term.String(FileName)
            ];
            if (decoded is not TupleTerm { Items: [_, _, TupleTerm { Items: [var forms, _] }] }
                || !Cons.Items(forms).OfType<TupleTerm>()
                    .Select(form => form.Items is [Atom { Name: "attribute" }, _, Atom { Name: "file" },
                        TupleTerm { Items: [var path, _] }] ? path : null)
                    .OfType<Term>().SequenceEqual(expected))
            {
                throw new InvalidOperationException(Failure);
            }
        }
        finally
        {
            temporary.Delete(recursive: true);
        }

        return Task.CompletedTask;
    }
}
