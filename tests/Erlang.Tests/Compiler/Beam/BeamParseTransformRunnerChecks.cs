using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamParseTransformRunnerChecks
{
    internal const string TestName = "compiler/beam/runs-generated-parse-transform-with-selected-dependencies";
    private const string Subject = "beam_transform_subject";
    private const string Transformer = "beam_fixture_transform";
    private const string Peer = "beam_fixture_transform_peer";
    private const string Marker = "transform_marker";
    private const string Option = "marker";
    private const string MissingOption = "missing_marker";
    private const string WrongForm = "Expected the selected peer to return the annotated attribute and the original forms.";
    private const string MissingError = "Expected the original Erlang parse transform exception.";
    private const string MissingDependency = "Expected the transformer startup to reject the undeclared peer import.";
    private const string StartupFailure = "Startup library selection failed: ";
    private const string UnsupportedImport = "The BEAM external import is outside the supported host subset";

    private const string TransformerSource = """
        -module(beam_fixture_transform).
        -export([parse_transform/2]).
        parse_transform(Forms, [{marker, Value}]) ->
            beam_fixture_transform_peer:insert(Forms, Value);
        parse_transform(_, []) ->
            throw(missing_marker).
        """;

    private const string PeerSource = """
        -module(beam_fixture_transform_peer).
        -export([insert/2]).
        insert(Forms, Value) ->
            [{attribute, {2,4}, transform_marker, {first, Value}} | Forms].
        """;

    internal static async Task Run()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        Term forms = ReadSubjectForms();
        byte[] transformer = CodeGeneration.CompileBeamModule(TransformerSource, Transformer + ".erl");
        byte[] firstPeer = CodeGeneration.CompileBeamModule(PeerSource, Peer + ".erl");
        string laterSource = PeerSource.Replace("first", "second", StringComparison.Ordinal);
        byte[] secondPeer = CodeGeneration.CompileBeamModule(laterSource, Peer + ".erl");
        var first = new RuntimeLibrarySelection(
        [
            RuntimeModuleSelection.Managed("erlang"),
            RuntimeModuleSelection.Beam(Transformer, transformer, [Peer, "erlang"]),
            RuntimeModuleSelection.Beam(Peer, firstPeer)
        ]);
        var second = new RuntimeLibrarySelection(
        [
            RuntimeModuleSelection.Managed("erlang"),
            RuntimeModuleSelection.Beam(Transformer, transformer, [Peer, "erlang"]),
            RuntimeModuleSelection.Beam(Peer, secondPeer)
        ]);

        Term value = Term.I(17);
        Term options = Term.List(Term.Tuple(Term.A(Option), value));
        Term annotatedPosition = Term.Tuple(Term.I(2), Term.I(4));

        Term Expected(string backend) => new Cons(
            Term.Tuple(
                Term.A("attribute"),
                annotatedPosition,
                Term.A(Marker),
                Term.Tuple(Term.A(backend), value)
            ),
            forms
        );
        if (!(await Invoke(first, options, deadline.Token)).Equals(Expected("first"))
            || !(await Invoke(second, options, deadline.Token)).Equals(Expected("second")))
        {
            throw new InvalidOperationException(WrongForm);
        }

        try
        {
            await Invoke(first, Nil.Value, deadline.Token);
            throw new InvalidOperationException(MissingError);
        }
        catch (ErlangException error) when (
            error.ExceptionClass == ErlangExceptionClasses.Throw
            && error.Reason.Equals(Term.A(MissingOption))
            && error.StackTraceTerm is Cons
        )
        {
        }

        var incomplete = new RuntimeLibrarySelection(
        [
            RuntimeModuleSelection.Managed("erlang"),
            RuntimeModuleSelection.Beam(Transformer, transformer, ["erlang"])
        ]);
        try
        {
            await Invoke(incomplete, options, deadline.Token);
            throw new InvalidOperationException(MissingDependency);
        }
        catch (InvalidOperationException error) when (
            error.Message == StartupFailure + Transformer
            && error.InnerException is NotSupportedException { Message: UnsupportedImport }
        )
        {
        }

        Task<Term> Invoke(RuntimeLibrarySelection selection, Term transformOptions, CancellationToken cancellation) =>
            BeamParseTransformRunner.InvokeAsync(
                selection,
                Transformer,
                forms,
                transformOptions,
                RuntimeNativePrimitives.None,
                cancellation
            );
    }

    private static Term ReadSubjectForms()
    {
        const string source = """
            -module(beam_transform_subject).
            -export([value/0]).
            value() -> ok.
            """;
        var directory = Directory.CreateTempSubdirectory("erlang-compilation-transform-");
        try
        {
            string path = Path.Combine(directory.FullName, Subject + ".erl");
            File.WriteAllText(path, source + "\n");
            var profile = new BeamReleaseBuildProfile(
                "10.0.6",
                path,
                directory.FullName,
                [],
                [],
                EmitDocumentation: false
            );
            byte[] image = CodeGeneration.CompileBeamFile(path, buildProfile: profile);
            byte[] debug = Chunks(image).Single(section => section.Id == "Dbgi").Data;
            if (ExternalTermFormat.Decode(RawEtf(debug)) is TupleTerm
                {
                    Items: [Atom { Name: "debug_info_v1" }, Atom { Name: "erl_abstract_code" },
                        TupleTerm { Items: [var forms, _] }]
                })
            {
                return forms;
            }

            throw new InvalidOperationException(WrongForm);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
