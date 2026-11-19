using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamInetsIncludeProfileChecks
{
    internal const string TestName = "compiler/beam/windows-inets-include-profile";

    private const string Application = "inets-9.8";
    private const string Module = "beam_inets_include_profile";
    private const string SourceFile = Module + ".erl";
    private const string BuildLibrary = "/daily_build/otp_src/lib/inets";
    private const string BootstrapKernel = "/daily_build/otp_src/bootstrap/lib/kernel";

    internal static Task Run()
    {
        var temporary = Directory.CreateTempSubdirectory("erlang-inets-profile-");
        try
        {
            Verify(temporary.FullName);
        }
        finally
        {
            temporary.Delete(recursive: true);
        }

        return Task.CompletedTask;
    }

    internal static void Verify(string root)
    {
        string library = Path.Combine(root, "lib");
        string inets = Path.Combine(library, Application);
        string source = Path.Combine(
            inets,
            "src",
            "http_server",
            SourceFile
        );
        Write(
            source,
            """
            -module(beam_inets_include_profile).
            -export([value/0]).
            -include("../http_lib/http_internal.hrl").
            -include("inets_internal.hrl").
            -include_lib("inets/include/mod_auth.hrl").
            -include_lib("kernel/include/logger.hrl").
            value() -> {?RELATIVE, ?LOCAL, ?APP, ?BOOT}.
            """
        );
        Write(Path.Combine(
            inets,
            "src",
            "http_lib",
            "http_internal.hrl"
        ), "-define(RELATIVE, relative).");
        Write(Path.Combine(
            inets,
            "src",
            "inets_app",
            "inets_internal.hrl"
        ), "-define(LOCAL, include_flag).");
        Write(Path.Combine(inets, "include", "mod_auth.hrl"), "-define(APP, application).");
        Write(Path.Combine(
            library,
            "kernel-11.0.4",
            "include",
            "logger.hrl"
        ), "-define(BOOT, bootstrap).");

        string relativeSource = Path.GetRelativePath(root, source);
        var profile = OtpWindowsReleaseProfiles.FindForSource(Application, relativeSource)
            ?? throw new InvalidOperationException("The Windows inets build profile was unavailable.");
        var paths = OtpWindowsInetsIncludePaths.Find(
            library,
            Application,
            source,
            profile,
            sourceHeaders: null
        ) ?? throw new InvalidOperationException("The Windows inets include path map was unavailable.");
        profile = profile with { IncludePathMap = paths };

        byte[] baseline = CompileAndCheck(
            source,
            profile,
            BuildLibrary + "/src/http_server/../inets_app/inets_internal.hrl"
        );
        File.WriteAllBytes(Path.Combine(root, "baseline.beam"), baseline);
        Write(Path.Combine(
            inets,
            "src",
            "http_server",
            "inets_internal.hrl"
        ), "-define(LOCAL, shadow).");
        byte[] shadow = CompileAndCheck(source, profile, "inets_internal.hrl");
        File.WriteAllBytes(Path.Combine(root, "shadow.beam"), shadow);
    }

    private static byte[] CompileAndCheck(
        string source,
        BeamReleaseBuildProfile profile,
        string localHeader
    )
    {
        byte[] beam = CodeGeneration.CompileBeamFile(source, buildProfile: profile);
        byte[] debug = Chunks(beam).Single(chunk => chunk.Id == "Dbgi").Data;
        if (ExternalTermFormat.Decode(RawEtf(debug)) is not TupleTerm
            {
                Items: [_, _, TupleTerm { Items: [var forms, _] }]
            })
        {
            throw new InvalidOperationException("Invalid debug information in Windows inets include fixture.");
        }

        var files = Cons.Items(forms).OfType<TupleTerm>()
            .Where(form => form.Items is [Atom { Name: "attribute" }, _, Atom { Name: "file" }, _])
            .Select(form => ((TupleTerm)form.Items[3]).Items[0]);
        string[] expected =
        [
            SourceFile,
            "../http_lib/http_internal.hrl",
            SourceFile,
            localHeader,
            SourceFile,
            BuildLibrary + "/include/mod_auth.hrl",
            SourceFile,
            BootstrapKernel + "/include/logger.hrl",
            SourceFile
        ];
        if (!files.SequenceEqual(expected.Select(path => (Term)Term.String(path))))
        {
            throw new InvalidOperationException("The Windows inets include origins did not match the release build.");
        }

        return beam;
    }

    private static void Write(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content + "\n");
    }
}
