using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamAbstractFirstAnnotationChecks
{
    internal const string TestName = "compiler/beam/abstract-expressions-use-first-annotation";
    private const string Module = "beam_first_annotation";
    private const string TraversalFailure = "Abstract annotation traversal did not stop at the first later source location.";
    private const string FormsFailure = "Expected match, case and if abstract forms to use the native parser's annotation.";

    internal static Task Run()
    {
        CheckTraversal();
        const string source = """
            -module(beam_first_annotation).
            -export([run/2]).
            run(Value, X) ->
                Result = case Value of
                    ({tag,A}) -> A;
                    (A) -> A
                end,
                (Result) = X,
                if
                    (X + 2) > 0 -> Result;
                    true -> 0
                end.
            """;
        var directory = Directory.CreateTempSubdirectory("erlang-first-annotation-");
        try
        {
            string path = Path.Combine(directory.FullName, Module + ".erl");
            File.WriteAllText(path, source + "\n");
            var profile = new BeamReleaseBuildProfile(
                "10.0.6",
                path,
                directory.FullName,
                [],
                [],
                EmitDocumentation: false
            );
            VerifyForms(CodeGeneration.CompileBeamFile(path, buildProfile: profile));
        }
        finally
        {
            directory.Delete(recursive: true);
        }

        return Task.CompletedTask;
    }

    private static void CheckTraversal()
    {
        Term annotation = Term.List(
            Term.Tuple(Term.A("generated"), Term.A("true")),
            Term.Tuple(Term.A("location"), Position(20, 5))
        );
        Term tuple = Term.Tuple(
            Term.A("tuple"),
            annotation,
            Term.List(
                Term.Tuple(Term.A("var"), Position(20, 6), Term.A("A")),
                Term.Tuple(Term.A("var"), Position(20, 1), Term.A("B"))
            )
        );
        Term op = Term.Tuple(
            Term.A("op"),
            Position(21, 9),
            Term.A("+"),
            Term.Tuple(Term.A("integer"), Position(21, 4), Term.I(0)),
            Term.Tuple(Term.A("integer"), Position(21, 12), Term.I(1))
        );
        if (!BeamAbstractFirstAnnotation.Read(tuple).Equals(annotation)
            || !BeamAbstractFirstAnnotation.Read(op).Equals(Position(21, 4)))
        {
            throw new InvalidOperationException(TraversalFailure);
        }
    }

    private static void VerifyForms(byte[] beam)
    {
        byte[] debug = Chunks(beam).Single(chunk => chunk.Id == "Dbgi").Data;
        if (ExternalTermFormat.Decode(RawEtf(debug)) is not TupleTerm
            {
                Items: [_, _, TupleTerm { Items: [var forms, _] }]
            })
        {
            throw new InvalidOperationException(FormsFailure);
        }

        var function = Cons.Items(forms).OfType<TupleTerm>().Single(form =>
            form.Items is [Atom { Name: "function" }, _, Atom { Name: "run" }, _, _]);
        var head = (TupleTerm)Cons.Items(function.Items[4]).Single();
        if (Cons.Items(head.Items[4]).ToArray() is not [TupleTerm binding, TupleTerm match, TupleTerm conditional]
            || binding.Items[3] is not TupleTerm decision
            || !match.Items[1].Equals(Position(8, 6)))
        {
            throw new InvalidOperationException(FormsFailure);
        }

        CheckClauseAnnotations(decision.Items[3], Position(5, 10), Position(6, 10));
        CheckClauseAnnotations(conditional.Items[2], Position(10, 10), Position(11, 9));
    }

    private static void CheckClauseAnnotations(Term clauses, params Term[] expected)
    {
        var actual = Cons.Items(clauses).Cast<TupleTerm>().Select(clause => clause.Items[1]);
        if (!actual.SequenceEqual(expected))
        {
            throw new InvalidOperationException(FormsFailure);
        }
    }

    private static Term Position(int line, int column) => Term.Tuple(Term.I(line), Term.I(column));
}
