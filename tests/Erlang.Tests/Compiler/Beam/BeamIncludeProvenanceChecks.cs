using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamIncludeProvenanceChecks
{
    internal const string TestName = "compiler/beam/record-epp-include-search-provenance";
    private const string MainFile = "beam_include_provenance.erl";
    private const string ReleaseInclude = "/release/app/src/../include";
    private const string InTreeLibrary = "/release/lib";
    private const string BootstrapLibrary = "/release/bootstrap/lib/example";

    internal static Task Run()
    {
        var temporary = Directory.CreateTempSubdirectory("erlang-include-provenance-");
        try
        {
            Verify(Compile(Path.Combine(temporary.FullName, "default")));
            Verify(Compile(Path.Combine(temporary.FullName, "projected"), useProjectedSourceDirectory: true));
        }
        finally
        {
            temporary.Delete(recursive: true);
        }

        return Task.CompletedTask;
    }

    internal static void Verify(byte[] beam)
    {
        byte[] chunk = Chunks(beam).Single(item => item.Id == "Dbgi").Data;
        Term decoded = ExternalTermFormat.Decode(RawEtf(chunk));
        if (decoded is not TupleTerm { Items: [_, _, TupleTerm { Items: [var forms, _] }] })
        {
            throw new InvalidOperationException("Invalid debug-info structure for include provenance fixture.");
        }

        Term[] files = Cons.Items(forms).OfType<TupleTerm>()
            .Where(form => form.Items is [Atom { Name: "attribute" }, _, Atom { Name: "file" }, _])
            .Select(form => ((TupleTerm)form.Items[3]).Items[0]).ToArray();
        Term[] expected = new[]
        {
            MainFile,
            "example/include/shadow.hrl",
            MainFile,
            ReleaseInclude + "/deep/parent.hrl",
            ReleaseInclude + "/deep/../actual.hrl",
            ReleaseInclude + "/deep/parent.hrl",
            MainFile,
            BootstrapLibrary + "/include/library_only.hrl",
            MainFile,
            InTreeLibrary + "/mapped/include/ordinary.hrl",
            MainFile,
            InTreeLibrary + "/mapped/include/shared.hrl",
            MainFile
        }.Select(path => (Term)Term.String(path)).ToArray();
        if (!files.SequenceEqual(expected))
        {
            throw new InvalidOperationException("The BEAM debug-info include paths do not follow EPP search order.");
        }

    }

    internal static byte[] Compile(string root, bool useProjectedSourceDirectory = false)
    {
        string installedSourceDirectory = Path.Combine(root, "src");
        string source = Path.Combine(installedSourceDirectory, MainFile);
        string sourceDirectory = useProjectedSourceDirectory
            ? Path.Combine(root, "pinned_src")
            : installedSourceDirectory;
        string include = Path.Combine(root, "physical_include");
        string secondInclude = Path.Combine(root, "second_include");
        string library = Path.Combine(root, "example-1.0");
        string physicalApps = Path.Combine(root, "installed");
        string mapped = Path.Combine(physicalApps, "mapped-2.0");
        string bootstrapMapped = Path.Combine(root, "bootstrap", "mapped");
        Write(
            source,
            """
            -module(beam_include_provenance).
            -export([value/0]).
            -include_lib("example/include/shadow.hrl").
            -include("deep/parent.hrl").
            -include_lib("example/include/library_only.hrl").
            -include("./mapped/include/ordinary.hrl").
            -include_lib("mapped/include/shared.hrl").
            value() -> {?SHADOW, ?ACTUAL, ?LIBRARY, ?ORDINARY, ?SHARED}.
            """
        );
        Write(Path.Combine(
            sourceDirectory,
            "example",
            "include",
            "shadow.hrl"
        ), "-define(SHADOW, local).");
        Write(Path.Combine(library, "include", "shadow.hrl"), "-define(SHADOW, wrong).");
        if (useProjectedSourceDirectory)
        {
            Write(Path.Combine(installedSourceDirectory, "deep", "parent.hrl"), "-define(ACTUAL, wrong).");
        }
        Write(Path.Combine(include, "deep", "parent.hrl"), "-include(\"../actual.hrl\").");
        Write(Path.Combine(include, "actual.hrl"), "-define(ACTUAL, first).");
        Write(Path.Combine(secondInclude, "actual.hrl"), "-define(ACTUAL, wrong).");
        Write(Path.Combine(library, "include", "library_only.hrl"), "-define(LIBRARY, fallback).");

        Write(Path.Combine(mapped, "include", "ordinary.hrl"), "-define(ORDINARY, in_tree).");
        Write(Path.Combine(mapped, "include", "shared.hrl"), "-define(SHARED, in_tree).");
        Write(Path.Combine(bootstrapMapped, "include", "shared.hrl"), "-define(SHARED, wrong).");

        var inTree = new BeamIncludeSearchDirectory(physicalApps, InTreeLibrary)
        {
            PhysicalChildDirectories = new Dictionary<string, string> { ["mapped"] = mapped }
        };
        var profile = new BeamReleaseBuildProfile(
            "10.0.6",
            "/release/app/src/" + MainFile,
            "/release/app/src",
            [ReleaseInclude],
            [],
            RecordedIncludeFiles: new Dictionary<string, string> { ["actual.hrl"] = "wrong.hrl" }
        )
        {
            IncludePathMap = new(
                MainFile,
                new(sourceDirectory, "."),
                [new(include, ReleaseInclude), new(secondInclude, "/release/other"), inTree],
                new Dictionary<string, BeamIncludeSearchDirectory>
                {
                    ["example"] = new(library, BootstrapLibrary),
                    ["mapped"] = new(bootstrapMapped, "/release/bootstrap/lib/mapped")
                }
            )
            {
                PhysicalSourceDirectory = useProjectedSourceDirectory ? sourceDirectory : null
            }
        };

        return CodeGeneration.CompileBeamFile(source, buildProfile: profile);
    }

    private static void Write(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content + "\n");
    }
}
