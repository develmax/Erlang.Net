using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamPublicKeyIncludeProfileChecks
{
    internal const string TestName = "compiler/beam/windows-public-key-source-overlay";
    private const string Application = OtpWindowsPublicKeyIncludePaths.Application;
    private const string SourceFile = "beam_public_key_include_profile.erl";

    internal static Task Run()
    {
        var temporary = Directory.CreateTempSubdirectory("erlang-public-key-include-");
        try
        {
            string library = Path.Combine(temporary.FullName, "lib");
            string app = Path.Combine(library, Application);
            string generated = Path.Combine(temporary.FullName, "generated");
            string source = Path.Combine(app, "src", SourceFile);
            Write(Path.Combine(generated, "generated.hrl"), "-define(GENERATED, generated).");
            Write(Path.Combine(app, "include", "generated.hrl"), "-error(source_overlay_must_win).");
            Write(Path.Combine(app, "include", "PKCS-FRAME.hrl"), "-define(FRAME, copied).");
            Write(Path.Combine(app, "include", "public_key.hrl"), "-define(PUBLIC, ordinary).");
            Write(Path.Combine(
                library,
                "kernel-11.0.4",
                "include",
                "file.hrl"
            ), "-define(KERNEL, bootstrap).");
            Write(
                source,
                """
                -module(beam_public_key_include_profile).
                -export([value/0]).
                -include("generated.hrl").
                -include("PKCS-FRAME.hrl").
                -include("public_key.hrl").
                -include_lib("kernel/include/file.hrl").
                value() -> {?GENERATED, ?FRAME, ?PUBLIC, ?KERNEL}.
                """
            );
            var profile = OtpWindowsReleaseProfiles.FindForSource(Application, Path.GetRelativePath(temporary.FullName, source))
                ?? throw new InvalidOperationException("The public_key release profile was unavailable.");
            using var overlay = OtpWindowsPublicKeyIncludePaths.Create(library, generated);
            var paths = overlay.Find(
                Application,
                source,
                profile,
                sourceHeaders: null
            ) ?? throw new InvalidOperationException("The public_key include overlay was unavailable.");
            profile = profile with { IncludePathMap = paths };
            Check(source, profile, "/daily_build/otp_src/bootstrap/lib/kernel/include/file.hrl");

            Write(Path.Combine(
                app,
                "src",
                "kernel",
                "include",
                "file.hrl"
            ), "-define(KERNEL, local).");
            Check(source, profile, "kernel/include/file.hrl");

            if (File.Exists(Path.Combine(generated, "PKCS-FRAME.hrl")))
            {
                throw new InvalidOperationException("The include overlay changed the supplied generated headers.");
            }
        }
        finally
        {
            temporary.Delete(recursive: true);
        }

        return Task.CompletedTask;
    }

    private static void Check(string source, BeamReleaseBuildProfile profile, string kernel)
    {
        byte[] beam = CodeGeneration.CompileBeamFile(source, buildProfile: profile);
        byte[] debug = Chunks(beam).Single(chunk => chunk.Id == "Dbgi").Data;
        if (ExternalTermFormat.Decode(RawEtf(debug)) is not TupleTerm
            {
                Items: [_, _, TupleTerm { Items: [var forms, _] }]
            })
        {
            throw new InvalidOperationException("Invalid debug information in the public_key overlay fixture.");
        }

        string[] expected =
        [
            SourceFile,
            "generated.hrl",
            SourceFile,
            "PKCS-FRAME.hrl",
            SourceFile,
            "/daily_build/otp_src/lib/public_key/src/../include/public_key.hrl",
            SourceFile,
            kernel,
            SourceFile
        ];
        var actual = Cons.Items(forms).OfType<TupleTerm>()
            .Where(form => form.Items is [Atom { Name: "attribute" }, _, Atom { Name: "file" }, _])
            .Select(form => ((TupleTerm)form.Items[3]).Items[0]);
        if (!actual.SequenceEqual(expected.Select(path => (Term)Term.String(path))))
        {
            throw new InvalidOperationException("The public_key include overlay did not preserve source identities.");
        }
    }

    private static void Write(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text + "\n");
    }
}
