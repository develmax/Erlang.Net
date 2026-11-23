using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamWxReleaseIncludeChecks
{
    internal const string TestName = "compiler/beam/windows-wx-include-source-precedence";
    private const string Application = "wx-2.7";
    private const string SourceName = "beam_wx_include_profile.erl";
    private const string InTreeHeader = "/daily_build/otp_src/lib/wx/include/wx.hrl";
    private const string Mismatch = "The Windows Wx include did not preserve the winning search location.";
    private const string Source = """
        -module(beam_wx_include_profile).
        -moduledoc false.
        -export_type([exported_type/0]).
        -include_lib("wx/include/wx.hrl").
        -include("wxe.hrl").
        -export([value/0]).
        value() -> {?PUBLIC, ?GENERATED}.
        """;

    internal static Task Run()
    {
        var temporary = Directory.CreateTempSubdirectory("erlang-wx-includes-");
        try
        {
            string library = Path.Combine(temporary.FullName, "lib");
            string wx = Path.Combine(library, Application);
            string sourceDirectory = Path.Combine(wx, "src");
            string generatedDirectory = Path.Combine(sourceDirectory, "gen");
            string localSource = Path.Combine(sourceDirectory, SourceName);
            string generatedSource = Path.Combine(generatedDirectory, SourceName);
            Write(Path.Combine(wx, "include", "wx.hrl"), "-define(PUBLIC, in_tree).\n-type exported_type() :: atom().");
            Write(Path.Combine(sourceDirectory, "wxe.hrl"), """-include("gen/wxe_funcs.hrl").""");
            Write(Path.Combine(generatedDirectory, "wxe_funcs.hrl"), "-define(GENERATED, generated).");
            Write(localSource, Source);
            Write(generatedSource, Source);

            var handwritten = ReadPaths(
                temporary.FullName,
                library,
                localSource,
                InTreeHeader
            );
            Verify(handwritten, SourceName, InTreeHeader);
            var generated = ReadPaths(
                temporary.FullName,
                library,
                generatedSource,
                InTreeHeader
            );
            Verify(generated, "gen/" + SourceName, InTreeHeader);

            string shadow = Path.Combine(generatedDirectory, "wx", "include");
            Write(Path.Combine(shadow, "wx.hrl"), "-define(PUBLIC, local).\n-type exported_type() :: atom().");
            generated = ReadPaths(
                temporary.FullName,
                library,
                generatedSource,
                "gen/wx/include/wx.hrl"
            );
            Verify(generated, "gen/" + SourceName, "gen/wx/include/wx.hrl");
        }
        finally
        {
            temporary.Delete(recursive: true);
        }

        return Task.CompletedTask;
    }

    private static IEnumerable<Term> ReadPaths(
        string release,
        string library,
        string source,
        string expectedHeader
    )
    {
        var profile = OtpWindowsReleaseProfiles.FindForSource(Application, Path.GetRelativePath(release, source))
            ?? throw new InvalidOperationException(Mismatch);
        var map = OtpWindowsInTreeLibraryIncludePaths.Find(
            library,
            Application,
            source,
            profile,
            sourceHeaders: null
        ) ?? throw new InvalidOperationException(Mismatch);
        byte[] beam = CodeGeneration.CompileBeamFile(source, library, buildProfile: profile with { IncludePathMap = map });
        var chunks = Chunks(beam);
        byte[] docs = chunks.Single(chunk => chunk.Id == "Docs").Data;
        if (ExternalTermFormat.Decode(RawEtf(docs)) is not TupleTerm { Items: [_, _, _, _, _, _, var entries] }
            || Cons.Items(entries).FirstOrDefault() is not TupleTerm
            {
                Items:
                [
                    TupleTerm { Items: [Atom { Name: "type" }, Atom { Name: "exported_type" }, _] },
                    Cons { Head: TupleTerm { Items: [Atom { Name: "file" }, var file] } },
                    ..
                ]
            }
            || !file.Equals(Term.String(expectedHeader)))
        {
            throw new InvalidOperationException(Mismatch);
        }

        byte[] debug = chunks.Single(chunk => chunk.Id == "Dbgi").Data;
        if (ExternalTermFormat.Decode(RawEtf(debug)) is not TupleTerm
            {
                Items: [_, _, TupleTerm { Items: [var forms, _] }]
            })
        {
            throw new InvalidOperationException(Mismatch);
        }

        return Cons.Items(forms).OfType<TupleTerm>()
            .Where(form => form.Items is [Atom { Name: "attribute" }, _, Atom { Name: "file" }, _])
            .Select(form => ((TupleTerm)form.Items[3]).Items[0]);
    }

    private static void Verify(IEnumerable<Term> paths, string source, string header)
    {
        string[] expected = [source, header, source, "wxe.hrl", "gen/wxe_funcs.hrl", "wxe.hrl", source];
        if (!paths.SequenceEqual(expected.Select(path => (Term)Term.String(path))))
        {
            throw new InvalidOperationException(Mismatch);
        }
    }

    private static void Write(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text + "\n");
    }
}
