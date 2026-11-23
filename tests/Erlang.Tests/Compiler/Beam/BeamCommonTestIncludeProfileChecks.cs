using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamCommonTestIncludeProfileChecks
{
    internal const string TestName = "compiler/beam/windows-common-test-include-provenance";
    private const string Application = "common_test-1.31.2";
    private const string FileName = "beam_common_test_includes.erl";
    private const string Build = "/daily_build/otp_src/";
    private const string Mismatch = "Common Test headers did not retain their winning source paths.";
    private const string Source = """
        -module(beam_common_test_includes).
        -include("snmp_types.hrl").
        -include("inet.hrl").
        -include("../../kernel/src/logger_internal.hrl").
        -include_lib("xmerl/include/xmerl.hrl").
        -export([value/0]).
        value() -> {?SNMP, ?INET, ?LOG, ?XML}.
        """;

    internal static Task Run()
    {
        var temporary = Directory.CreateTempSubdirectory("erlang-common-test-source-");
        try
        {
            string release = temporary.FullName;
            string library = Path.Combine(release, "lib");
            string headers = Path.Combine(release, "source");
            string source = Path.Combine(
                library,
                Application,
                "src",
                FileName
            );
            Directory.CreateDirectory(Path.Combine(
                headers,
                "lib",
                "common_test",
                "src"
            ));
            Write(source, Source);
            WriteHeader(library, "snmp-5.20.5/include/snmp_types.hrl", "-define(SNMP, snmp).");
            WriteHeader(library, "kernel-11.0.4/include/inet.hrl", "-define(INET, inet).");
            WriteHeader(library, "kernel-11.0.4/include/logger.hrl", "-define(LOG, logger).");
            WriteHeader(library, "xmerl-2.2/include/xmerl.hrl", "-define(XML, xml).");
            WriteHeader(headers, "lib/kernel/src/logger_internal.hrl", """-include_lib("kernel/include/logger.hrl").""");
            WriteHeader(headers, "lib/xmerl/inc/snmp_types.hrl", "-error(earlier_include_must_win).");

            var profile = OtpWindowsReleaseProfiles.FindForSource(Application, Path.GetRelativePath(release, source))
                ?? throw new InvalidOperationException(Mismatch);
            var map = OtpWindowsBootstrapLibraryIncludePaths.Find(
                library,
                Application,
                source,
                profile,
                headers
            ) ?? throw new InvalidOperationException(Mismatch);
            byte[] beam = CodeGeneration.CompileBeamFile(source, library, buildProfile: profile with { IncludePathMap = map });
            byte[] debug = Chunks(beam).Single(chunk => chunk.Id == "Dbgi").Data;
            if (ExternalTermFormat.Decode(RawEtf(debug)) is not TupleTerm
                {
                    Items: [_, _, TupleTerm { Items: [var forms, _] }]
                })
            {
                throw new InvalidOperationException(Mismatch);
            }

            string[] expected =
            [
                FileName,
                Build + "lib/snmp/include/snmp_types.hrl", FileName,
                Build + "lib/kernel/include/inet.hrl", FileName,
                "../../kernel/src/logger_internal.hrl",
                Build + "bootstrap/lib/kernel/include/logger.hrl", "../../kernel/src/logger_internal.hrl", FileName,
                Build + "bootstrap/lib/xmerl/include/xmerl.hrl", FileName
            ];
            var paths = Cons.Items(forms).OfType<TupleTerm>()
                .Where(form => form.Items is [Atom { Name: "attribute" }, _, Atom { Name: "file" }, _])
                .Select(form => ((TupleTerm)form.Items[3]).Items[0]);
            if (!paths.SequenceEqual(expected.Select(path => (Term)Term.String(path))))
            {
                throw new InvalidOperationException(Mismatch);
            }
        }
        finally
        {
            temporary.Delete(recursive: true);
        }

        return Task.CompletedTask;
    }

    private static void WriteHeader(string root, string path, string text) =>
        Write(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)), text);

    private static void Write(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text + "\n");
    }
}
