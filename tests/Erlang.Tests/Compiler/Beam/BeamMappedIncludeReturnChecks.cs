using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamMappedIncludeReturnChecks
{
    internal const string TestName = "compiler/beam/restore-physical-and-logical-include-parents";
    private const string SourceFile = "beam_epp_restore.erl";

    internal static Task Run()
    {
        var root = Directory.CreateTempSubdirectory("erlang-epp-restore-");
        try
        {
            Verify(root.FullName);
        }
        finally
        {
            root.Delete(recursive: true);
        }

        return Task.CompletedTask;
    }

    private static void Verify(string root)
    {
        string source = Path.Combine(root, SourceFile);
        Write(
            source,
            """
            -module(beam_epp_restore).
            -export([value/0, trigger/0]).
            -include("plain.hrl").
            -file("virtual.yrl", 100).
            -include("parent.hrl").
            value() -> {?PLAIN, ?PARENT, ?CHILD, ?LINE}.
            trigger() -> erlang:error(mapped).
            -file("beam_epp_restore.erl", 300).
            -include("same_file.hrl").
            """
        );
        Write(Path.Combine(root, "plain.hrl"), "-define(PLAIN, plain).");
        Write(
            Path.Combine(root, "parent.hrl"),
            """
            -file("header.yrl", 200).
            -include("child.hrl").
            -define(PARENT, parent).
            """
        );
        Write(Path.Combine(root, "child.hrl"), "-define(CHILD, child).");
        Write(Path.Combine(root, "same_file.hrl"), "% Keep the original physical source filename.");

        var profile = new BeamReleaseBuildProfile(
            "10.0.6",
            "/release/src/" + SourceFile,
            "/release/src",
            [],
            []
        );
        byte[] beam = CodeGeneration.CompileBeamFile(source, buildProfile: profile);
        byte[] debug = Chunks(beam).Single(chunk => chunk.Id == "Dbgi").Data;
        if (ExternalTermFormat.Decode(RawEtf(debug)) is not TupleTerm
            {
                Items: [_, _, TupleTerm { Items: [var forms, _] }]
            })
        {
            throw new InvalidOperationException("Invalid debug information for mapped include restoration.");
        }

        var tuples = Cons.Items(forms).OfType<TupleTerm>().ToArray();
        var fileForms = tuples.Where(form => form.Items is [Atom { Name: "attribute" }, _, Atom { Name: "file" }, _]);
        Term[] expected =
        [
            File(SourceFile, 1),
            File("plain.hrl", 1),
            File(SourceFile, 4),
            File("virtual.yrl", 100, Location(4, 2)),
            File("parent.hrl", 1),
            File("header.yrl", 200, Location(1, 2)),
            File("child.hrl", 1),
            File("parent.hrl", 3),
            File("header.yrl", 202, Location(3, 1)),
            File(SourceFile, 6),
            File("virtual.yrl", 102, Location(6, 1)),
            File(SourceFile, 300, Location(104, 2)),
            File("same_file.hrl", 1),
            File(SourceFile, 10)
        ];
        if (!fileForms.SequenceEqual(expected))
        {
            throw new InvalidOperationException("Included files did not restore the physical and declared parent locations.");
        }

        var functions = tuples.Where(form => form.Items is [Atom { Name: "function" }, _, _, _, _]).ToArray();
        if (functions.Length != 2 || !functions[0].Items[1].Equals(Location(102, 1))
            || !functions[1].Items[1].Equals(Location(103, 1)))
        {
            throw new InvalidOperationException("Include restoration changed the executable source locations.");
        }
    }

    private static Term Location(int line, int column) => Term.Tuple(Term.I(line), Term.I(column));

    private static Term File(string path, int line, Term? location = null) =>
        Term.Tuple(
            Term.A("attribute"),
            location is null
                ? Location(line, 1)
                : Term.List(Term.Tuple(Term.A("generated"), Term.A("true")), Term.Tuple(Term.A("location"), location)),
            Term.A("file"),
            Term.Tuple(Term.String(path), Term.I(line))
        );

    private static void Write(string path, string content) => System.IO.File.WriteAllText(path, content + "\n");
}
