using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamStdlibIncludeProfileChecks
{
    internal const string TestName = "compiler/beam/windows-stdlib-bootstrap-include-profile";

    private const string Application = "stdlib-8.1";
    private const string Module = "beam_stdlib_include_profile";
    private const string SourceFile = Module + ".erl";
    private const string Build = "/daily_build/otp_src/lib/stdlib/src";
    private const string Bootstrap = "/daily_build/otp_src/bootstrap/lib/";

    internal static Task Run()
    {
        var temporary = Directory.CreateTempSubdirectory("erlang-stdlib-profile-");
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
        string stdlib = Path.Combine(library, Application);
        string kernel = Path.Combine(library, "kernel-11.0.4");
        string source = Path.Combine(stdlib, "src", SourceFile);
        Write(
            source,
            """
            -module(beam_stdlib_include_profile).
            -export([value/0]).
            -include("ordered.hrl").
            -include("file.hrl").
            -include_lib("kernel/include/logger.hrl").
            -include_lib("stdlib/include/qlc.hrl").
            value() -> {?ORDERED, ?FILE_KIND, ?LOGGER, ?QLC}.
            """
        );
        Write(Path.Combine(stdlib, "include", "ordered.hrl"), "-define(ORDERED, stdlib_first).");
        Write(Path.Combine(kernel, "include", "ordered.hrl"), "-define(ORDERED, kernel_second).");
        Write(Path.Combine(kernel, "include", "file.hrl"), "-define(FILE_KIND, include_flag).");
        Write(Path.Combine(kernel, "include", "logger.hrl"), "-define(LOGGER, kernel_bootstrap).");
        Write(Path.Combine(stdlib, "include", "qlc.hrl"), "-define(QLC, stdlib_bootstrap).");

        string relativeSource = Path.GetRelativePath(root, source);
        var profile = OtpWindowsReleaseProfiles.FindForSource(Application, relativeSource)
            ?? throw new InvalidOperationException("The Windows stdlib build profile was unavailable.");
        var includePaths = OtpWindowsCoreIncludePaths.Find(
            library,
            Application,
            source,
            profile,
            sourceHeaders: null
        ) ?? throw new InvalidOperationException("The Windows stdlib include path map was unavailable.");
        byte[] beam = CodeGeneration.CompileBeamFile(source, buildProfile: profile with { IncludePathMap = includePaths });
        byte[] chunk = Chunks(beam).Single(item => item.Id == "Dbgi").Data;
        if (ExternalTermFormat.Decode(RawEtf(chunk)) is not TupleTerm
            {
                Items: [_, _, TupleTerm { Items: [var forms, _] }]
            })
        {
            throw new InvalidOperationException("Invalid debug information in the Windows stdlib fixture.");
        }

        var files = Cons.Items(forms).OfType<TupleTerm>()
            .Where(form => form.Items is [Atom { Name: "attribute" }, _, Atom { Name: "file" }, _])
            .Select(form => ((TupleTerm)form.Items[3]).Items[0]);
        string[] expected =
        [
            SourceFile,
            Build + "/../include/ordered.hrl",
            SourceFile,
            Build + "/../../kernel/include/file.hrl",
            SourceFile,
            Bootstrap + "kernel/include/logger.hrl",
            SourceFile,
            Bootstrap + "stdlib/include/qlc.hrl",
            SourceFile
        ];
        if (!files.SequenceEqual(expected.Select(path => (Term)Term.String(path))))
        {
            throw new InvalidOperationException("The Windows stdlib include origins did not match the release build.");
        }

        File.WriteAllBytes(Path.Combine(root, Module + ".beam"), beam);
    }

    private static void Write(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content + "\n");
    }
}
