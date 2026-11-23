using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamReleaseLibraryIncludeChecks
{
    internal const string TestName = "compiler/beam/release-debug-records-library-include-root";
    private const string Module = "beam_library_include";
    private const string FileName = Module + ".erl";
    private const string LibraryRoot = "/release/lib/beam_library";
    private const string OverridePath = "/override/special.hrl";
    private const string Failure = "The release debug metadata did not record the requested library include paths.";
    private const string Source = """
        -module(beam_library_include).
        -include("local.hrl").
        -include_lib("beam_library/include/remote.hrl").
        -include_lib("beam_library/include/special.hrl").
        -include_lib("other_library/include/other.hrl").
        -export([value/0]).
        value() -> ok.
        """;

    internal static Task Run()
    {
        var temporary = Directory.CreateTempSubdirectory("erlang-beam-library-include-");
        try
        {
            string directory = temporary.FullName;
            string source = Path.Combine(directory, FileName);
            File.WriteAllText(source, Source);
            File.WriteAllText(Path.Combine(directory, "local.hrl"), "-record(local, {value}).\n");
            WriteHeader("beam_library", "remote");
            WriteHeader("beam_library", "special");
            WriteHeader("other_library", "other");
            var profile = new BeamReleaseBuildProfile(
                "10.0.6",
                "/release/" + FileName,
                "/release",
                [],
                [],
                RecordedIncludeFiles: new Dictionary<string, string> { ["beam_library/include/special.hrl"] = OverridePath }
            )
            {
                RecordedIncludeLibraryRoots = new Dictionary<string, string> { ["beam_library"] = LibraryRoot }
            };
            byte[] image = CodeGeneration.CompileBeamFile(source, directory, buildProfile: profile);
            byte[] debug = Chunks(image).Single(chunk => chunk.Id == "Dbgi").Data;
            Term decoded = ExternalTermFormat.Decode(RawEtf(debug));
            Term[] expected =
            [
                Term.String(FileName),
                Term.String("local.hrl"),
                Term.String(FileName),
                Term.String(LibraryRoot + "/include/remote.hrl"),
                Term.String(FileName),
                Term.String(OverridePath),
                Term.String(FileName),
                Term.String("other.hrl"),
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

            void WriteHeader(string application, string header)
            {
                string includes = Path.Combine(
                    directory,
                    application,
                    "include"
                );
                Directory.CreateDirectory(includes);
                File.WriteAllText(Path.Combine(includes, header + ".hrl"), $"-record({header}, {{value}}).\n");
            }
        }
        finally
        {
            temporary.Delete(recursive: true);
        }

        return Task.CompletedTask;
    }
}
