using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamKernelIncludeProfileChecks
{
    internal const string TestName = "compiler/beam/windows-kernel-bootstrap-include-profile";

    private const string Kernel = "kernel-11.0.4";
    private const string Module = "beam_kernel_include_profile";
    private const string SourceFile = Module + ".erl";
    private const string Bootstrap = "/daily_build/otp_src/bootstrap/lib/";
    private const string BuildInclude = "/daily_build/otp_src/lib/kernel/src/../include/";

    internal static Task Run()
    {
        var temporary = Directory.CreateTempSubdirectory("erlang-kernel-profile-");
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
        string relativeSource = Path.Combine(
            "lib",
            Kernel,
            "src",
            SourceFile
        );
        string source = Path.Combine(root, relativeSource);
        string library = Path.Combine(root, "lib");
        string kernel = Path.Combine(library, Kernel);
        string stdlib = Path.Combine(library, "stdlib-8.1");
        string sourceHeaders = Path.Combine(root, "source-headers");
        Write(
            source,
            """
            -module(beam_kernel_include_profile).
            -export([value/0]).
            -include("file.hrl").
            -include("source_only.hrl").
            -include_lib("kernel/include/logger.hrl").
            -include_lib("stdlib/include/ms_transform.hrl").
            value() -> {?FILE_KIND, ?SOURCE_KIND, ?LOGGER_KIND, ?TRANSFORM_KIND}.
            """
        );
        Write(Path.Combine(kernel, "include", "file.hrl"), "-define(FILE_KIND, include_flag).");
        Write(Path.Combine(
            sourceHeaders,
            "lib",
            "kernel",
            "src",
            "source_only.hrl"
        ), "-define(SOURCE_KIND, cwd).");
        Write(Path.Combine(kernel, "include", "logger.hrl"), "-define(LOGGER_KIND, bootstrap).");
        Write(Path.Combine(stdlib, "include", "ms_transform.hrl"), "-define(TRANSFORM_KIND, bootstrap).");

        var profile = OtpWindowsReleaseProfiles.FindForSource(Kernel, relativeSource)
            ?? throw new InvalidOperationException("The Windows kernel build profile was unavailable.");
        var paths = OtpWindowsCoreIncludePaths.Find(
            library,
            Kernel,
            source,
            profile,
            sourceHeaders
        ) ?? throw new InvalidOperationException("The Windows kernel include path map was unavailable.");
        profile = profile with { IncludePathMap = paths };

        byte[] baseline = CompileAndCheck(
            source,
            profile,
            Bootstrap + "kernel/include/logger.hrl"
        );
        File.WriteAllBytes(Path.Combine(root, "baseline.beam"), baseline);
        Write(
            Path.Combine(
                kernel,
                "src",
                "kernel",
                "include",
                "logger.hrl"
            ),
            "-define(LOGGER_KIND, shadow)."
        );
        byte[] shadow = CompileAndCheck(
            source,
            profile,
            "kernel/include/logger.hrl"
        );
        File.WriteAllBytes(Path.Combine(root, "shadow.beam"), shadow);
    }

    private static byte[] CompileAndCheck(
        string source,
        BeamReleaseBuildProfile profile,
        string loggerFile
    )
    {
        byte[] beam = CodeGeneration.CompileBeamFile(source, buildProfile: profile);
        byte[] debug = Chunks(beam).Single(chunk => chunk.Id == "Dbgi").Data;
        if (ExternalTermFormat.Decode(RawEtf(debug)) is not TupleTerm
            {
                Items: [_, _, TupleTerm { Items: [var forms, _] }]
            })
        {
            throw new InvalidOperationException("Invalid debug information in Windows kernel profile fixture.");
        }

        var files = Cons.Items(forms).OfType<TupleTerm>()
            .Where(form => form.Items is [Atom { Name: "attribute" }, _, Atom { Name: "file" }, _])
            .Select(form => ((TupleTerm)form.Items[3]).Items[0]);
        string[] expected =
        [
            SourceFile,
            BuildInclude + "file.hrl",
            SourceFile,
            "source_only.hrl",
            SourceFile,
            loggerFile,
            SourceFile,
            Bootstrap + "stdlib/include/ms_transform.hrl",
            SourceFile
        ];
        if (!files.SequenceEqual(expected.Select(path => (Term)Term.String(path))))
        {
            throw new InvalidOperationException("The Windows kernel include origins did not match the release build.");
        }

        return beam;
    }

    private static void Write(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text + "\n");
    }
}
