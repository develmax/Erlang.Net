using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamBootstrapLibraryIncludeChecks
{
    internal const string TestName = "compiler/beam/windows-bootstrap-library-include-paths";

    private const string Module = "beam_bootstrap_include_profile";
    private const string SourceFile = Module + ".erl";
    private const string Bootstrap = "/daily_build/otp_src/bootstrap/lib/";

    internal static Task Run()
    {
        var temporary = Directory.CreateTempSubdirectory("erlang-bootstrap-includes-");
        try
        {
            Verify(
                temporary.FullName,
                "sasl-4.4",
                [
                "erl_compile.hrl",
                "/daily_build/otp_src/lib/sasl/src/../../stdlib/include/erl_compile.hrl"
            ],
                library: false
            );
            Verify(
                temporary.FullName,
                "tools-4.2.3",
                [
                "public_key/include/public_key.hrl",
                Bootstrap + "public_key/include/public_key.hrl"
            ],
                library: true
            );
            Verify(
                temporary.FullName,
                "eunit-2.11",
                [
                    "stdlib/include/assert.hrl",
                    "../../stdlib/include/assert.hrl"
                ],
                library: true
            );
            Verify(
                temporary.FullName,
                "erts-17.1",
                [
                    "file_int.hrl",
                    "/daily_build/otp_src/lib/kernel/src/file_int.hrl"
                ],
                library: false
            );
            Verify(
                temporary.FullName,
                "runtime_tools-2.5",
                [
                    "observer_backend.hrl",
                    "/daily_build/otp_src/lib/runtime_tools/src/../include/observer_backend.hrl"
                ],
                library: false
            );
            Verify(
                temporary.FullName,
                "ssh-6.0.6",
                [
                    "kernel/include/inet.hrl",
                    Bootstrap + "kernel/include/inet.hrl"
                ],
                library: true
            );
            Verify(
                temporary.FullName,
                "compiler-10.0.6",
                [
                    "erl_compile.hrl",
                    "/daily_build/otp_src/lib/compiler/src/../../stdlib/include/erl_compile.hrl"
                ],
                library: false
            );
            Verify(
                Path.Combine(temporary.FullName, "compiler-include-lib"),
                "compiler-10.0.6",
                [
                    "kernel/include/eep48.hrl",
                    Bootstrap + "kernel/include/eep48.hrl"
                ],
                library: true
            );
            Verify(
                Path.Combine(temporary.FullName, "megaco-engine-include-lib"),
                "megaco-4.9.2",
                [
                "et/include/et.hrl",
                "/daily_build/otp_src/lib/et/include/et.hrl"
                ],
                library: true,
                sourceDirectory: "engine"
            );
            Verify(
                Path.Combine(temporary.FullName, "megaco-engine-include"),
                "megaco-4.9.2",
                [
                "megaco.hrl",
                "/daily_build/otp_src/lib/megaco/src/engine/../../include/megaco.hrl"
                ],
                library: false,
                sourceDirectory: "engine"
            );
            Verify(
                Path.Combine(temporary.FullName, "diameter-transport"),
                "diameter-2.7.2",
                ["kernel/include/inet_sctp.hrl", Bootstrap + "kernel/include/inet_sctp.hrl"],
                library: true,
                sourceDirectory: "transport"
            );
            Verify(
                Path.Combine(temporary.FullName, "ssl-distribution"),
                "ssl-11.7.7",
                ["kernel/include/dist.hrl", Bootstrap + "kernel/include/dist.hrl"],
                library: true
            );
            Verify(
                Path.Combine(temporary.FullName, "leex"),
                "parsetools-2.8",
                ["erl_compile.hrl", "/daily_build/otp_src/lib/stdlib/include/erl_compile.hrl"],
                library: false
            );
            Verify(
                Path.Combine(temporary.FullName, "syntax-tools"),
                "syntax_tools-4.1.1",
                ["merl.hrl", "/daily_build/otp_src/lib/syntax_tools/src/../include/merl.hrl"],
                library: false
            );
            VerifyGeneratedHeaderOrder(Path.Combine(temporary.FullName, "eldap"));
            VerifyPrivateHeader(Path.Combine(temporary.FullName, "os-mon"));
        }
        finally
        {
            temporary.Delete(recursive: true);
        }

        return Task.CompletedTask;
    }

    private static void Verify(
        string root,
        string application,
        string[] include,
        bool library,
        string? sourceDirectory = null
    )
    {
        string releaseLib = Path.Combine(root, "lib");
        string source = Path.Combine(
            releaseLib,
            application,
            "src",
            sourceDirectory ?? "",
            SourceFile
        );
        Write(Path.Combine(
            releaseLib,
            "kernel-11.0.4",
            "include",
            "file.hrl"
        ), "-define(KERNEL, bootstrap).");
        Write(Path.Combine(
            releaseLib,
            "stdlib-8.1",
            "include",
            "erl_compile.hrl"
        ), "-define(EXTRA, stdlib).");
        Write(Path.Combine(
            releaseLib,
            "public_key-1.21.7",
            "include",
            "public_key.hrl"
        ), "-define(EXTRA, public_key).");
        Write(Path.Combine(
            releaseLib,
            "stdlib-8.1",
            "include",
            "assert.hrl"
        ), "-define(EXTRA, assertion).");
        Write(Path.Combine(
            releaseLib,
            "kernel-11.0.4",
            "src",
            "file_int.hrl"
        ), "-define(EXTRA, kernel_source).");
        Write(Path.Combine(
            releaseLib,
            "runtime_tools-2.5",
            "include",
            "observer_backend.hrl"
        ), "-define(EXTRA, observer_backend).");
        Write(Path.Combine(
            releaseLib,
            "et-1.8",
            "include",
            "observer_backend.hrl"
        ), "-error(et_should_not_shadow_the_local_include).");
        Write(Path.Combine(
            releaseLib,
            "kernel-11.0.4",
            "include",
            "inet.hrl"
        ), "-define(EXTRA, inet).");
        Write(Path.Combine(
            releaseLib,
            "kernel-11.0.4",
            "include",
            "eep48.hrl"
        ), "-define(EXTRA, eep48).");
        if (sourceDirectory == "transport")
        {
            Write(Path.Combine(
                releaseLib,
                "kernel-11.0.4",
                "include",
                "inet_sctp.hrl"
            ), "-define(EXTRA, sctp).");
        }

        if (application == "ssl-11.7.7")
        {
            Write(Path.Combine(
                releaseLib,
                "kernel-11.0.4",
                "include",
                "dist.hrl"
            ), "-define(EXTRA, distribution).");
        }

        if (application == "syntax_tools-4.1.1")
        {
            Write(Path.Combine(
                releaseLib,
                application,
                "include",
                "merl.hrl"
            ), "-define(EXTRA, merl).");
        }

        if (sourceDirectory == "engine")
        {
            Write(Path.Combine(
                releaseLib,
                "et-1.8",
                "include",
                "et.hrl"
            ), "-define(EXTRA, event_tracer).");
            Write(Path.Combine(
                releaseLib,
                application,
                "include",
                "megaco.hrl"
            ), "-define(EXTRA, megaco).");
        }

        string directive = library ? "include_lib" : "include";
        Write(
            source,
            $$"""
            -module({{Module}}).
            -export([value/0]).
            -include_lib("kernel/include/file.hrl").
            -{{directive}}("{{include[0]}}").
            value() -> {?KERNEL, ?EXTRA}.
            """
        );
        string relative = Path.GetRelativePath(root, source);
        var profile = OtpWindowsReleaseProfiles.FindForSource(application, relative)
            ?? throw new InvalidOperationException("The release profile was unavailable for the bootstrap include fixture.");
        var paths = OtpWindowsBootstrapLibraryIncludePaths.Find(
            releaseLib,
            application,
            source,
            profile,
            sourceHeaders: null
        ) ?? throw new InvalidOperationException("The bootstrap include profile was unavailable.");
        profile = profile with { IncludePathMap = paths };
        string recordedSource = profile.RecordedSourceFile ?? SourceFile;
        Check(
            source,
            profile,
            Bootstrap + "kernel/include/file.hrl",
            include[1],
            recordedSource
        );

        string localSourceDirectory = Path.GetDirectoryName(source)!;
        Write(
            Path.Combine(
                localSourceDirectory,
                "kernel",
                "include",
                "file.hrl"
            ),
            "-define(KERNEL, local)."
        );
        Check(
            source,
            profile,
            sourceDirectory == "transport" ? "transport/kernel/include/file.hrl" : "kernel/include/file.hrl",
            include[1],
            recordedSource
        );

        if (sourceDirectory == "engine")
        {
            Write(
                Path.Combine(localSourceDirectory, include[0]),
                "-define(EXTRA, local)."
            );
            Check(
                source,
                profile,
                "kernel/include/file.hrl",
                include[0]
            );
        }
    }

    private static void VerifyGeneratedHeaderOrder(string root)
    {
        const string application = "eldap-1.3.1";
        const string sourceName = "eldap.erl";
        const string include = "/daily_build/otp_src/lib/eldap/src/../include/";
        const string ebin = "/daily_build/otp_src/lib/eldap/src/../ebin/";
        string library = Path.Combine(root, "lib");
        string app = Path.Combine(library, application);
        string source = Path.Combine(app, "src", sourceName);
        Write(
            source,
            """
            -module(eldap).
            -export([value/0]).
            -include("ELDAPv3.hrl").
            -include("eldap.hrl").
            value() -> {?GENERATED, ?PUBLIC}.
            """
        );
        Write(Path.Combine(app, "ebin", "ELDAPv3.hrl"), "-define(GENERATED, asn1).");
        Write(Path.Combine(app, "include", "eldap.hrl"), "-define(PUBLIC, app).");
        Write(Path.Combine(app, "ebin", "eldap.hrl"), "-error(ebin_should_follow_include).");
        var profile = ReadProfile(
            root,
            library,
            application,
            source
        );
        Check(
            source,
            profile,
            ebin + "ELDAPv3.hrl",
            include + "eldap.hrl",
            sourceName
        );
    }

    private static void VerifyPrivateHeader(string root)
    {
        const string application = "os_mon-2.12";
        const string sourceName = "memsup.erl";
        string library = Path.Combine(root, "lib");
        string app = Path.Combine(library, application);
        string source = Path.Combine(app, "src", sourceName);
        string headers = Path.Combine(root, "pinned");
        Write(
            source,
            """
            -module(memsup).
            -export([value/0]).
            -include_lib("kernel/include/file.hrl").
            -include("memsup.hrl").
            value() -> {?KERNEL, ?PRIVATE}.
            """
        );
        Write(Path.Combine(app, "src", "memsup.hrl"), "-error(installed_source_copy_must_not_win).");
        Write(Path.Combine(
            headers,
            "lib",
            "os_mon",
            "include",
            "memsup.hrl"
        ), "-define(PRIVATE, build_include).");
        Write(Path.Combine(
            library,
            "kernel-11.0.4",
            "include",
            "file.hrl"
        ), "-define(KERNEL, bootstrap).");
        var profile = ReadProfile(
            root,
            library,
            application,
            source,
            headers
        );
        Check(
            source,
            profile,
            Bootstrap + "kernel/include/file.hrl",
            "/daily_build/otp_src/lib/os_mon/src/../include/memsup.hrl",
            sourceName
        );
    }

    private static BeamReleaseBuildProfile ReadProfile(
        string root,
        string library,
        string application,
        string source,
        string? headers = null
    )
    {
        var profile = OtpWindowsReleaseProfiles.FindForSource(application, Path.GetRelativePath(root, source))
            ?? throw new InvalidOperationException("The fixture build profile was unavailable.");
        var paths = OtpWindowsBootstrapLibraryIncludePaths.Find(
            library,
            application,
            source,
            profile,
            headers
        ) ?? throw new InvalidOperationException("The fixture include profile was unavailable.");

        return profile with { IncludePathMap = paths };
    }

    private static void Check(
        string source,
        BeamReleaseBuildProfile profile,
        string kernelPath,
        string otherPath,
        string recordedSource = SourceFile
    )
    {
        byte[] beam = CodeGeneration.CompileBeamFile(source, buildProfile: profile);
        byte[] debug = Chunks(beam).Single(chunk => chunk.Id == "Dbgi").Data;
        if (ExternalTermFormat.Decode(RawEtf(debug)) is not TupleTerm
            {
                Items: [_, _, TupleTerm { Items: [var forms, _] }]
            })
        {
            throw new InvalidOperationException("Invalid debug information in the bootstrap include fixture.");
        }

        var actual = Cons.Items(forms).OfType<TupleTerm>()
            .Where(form => form.Items is [Atom { Name: "attribute" }, _, Atom { Name: "file" }, _])
            .Select(form => ((TupleTerm)form.Items[3]).Items[0]);
        string[] expected = [recordedSource, kernelPath, recordedSource, otherPath, recordedSource];
        if (!actual.SequenceEqual(expected.Select(path => (Term)Term.String(path))))
        {
            throw new InvalidOperationException("The Windows bootstrap include paths did not follow EPP search order.");
        }
    }

    private static void Write(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text + "\n");
    }
}
