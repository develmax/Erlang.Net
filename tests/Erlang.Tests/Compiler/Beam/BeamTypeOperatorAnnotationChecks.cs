using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamTypeOperatorAnnotationChecks
{
    internal const string TestName = "compiler/beam/type-operator-source-annotations";
    private const string Module = "beam_type_operator_annotation";
    private const string FileName = Module + ".erl";
    private const string Application = "stdlib-8.1";

    internal static Task Run()
    {
        var temporary = Directory.CreateTempSubdirectory("erlang-type-operator-");
        try
        {
            string source = Path.Combine(temporary.FullName, FileName);
            File.WriteAllText(
                source,
                """
                -module(beam_type_operator_annotation).
                -export([value/0]).
                -export_type([direct/0, expanded/0]).
                -define(BIT(Bits), (1 bsl (Bits))).
                -define(MASK(Bits), (?BIT(Bits) - 1)).
                -type direct() :: 0..((1 bsl 5) - 1 + 2 * 3).
                -type expanded() :: 0..?MASK(32).
                -spec value() -> -1..(8 - 2 * 3).
                value() -> 2.
                """ + "\n"
            );
            var profile = OtpWindowsReleaseProfiles.FindForSource(Application, "lib/" + Application + "/src/" + FileName)
                ?? throw new InvalidOperationException("The type operator release profile was unavailable.");
            byte[] beam = CodeGeneration.CompileBeamFile(source, buildProfile: profile);
            byte[] debug = Chunks(beam).Single(chunk => chunk.Id == "Dbgi").Data;
            if (ExternalTermFormat.Decode(RawEtf(debug)) is not TupleTerm
                {
                    Items: [_, _, TupleTerm { Items: [var forms, _] }]
                })
            {
                throw new InvalidOperationException("Invalid debug information in the type operator fixture.");
            }

            var attributes = Cons.Items(forms).OfType<TupleTerm>().ToArray();
            Term direct = Type(attributes, "direct");
            Term expanded = Type(attributes, "expanded");
            var spec = attributes.Single(form => form.Items is [_, _, Atom { Name: "spec" }, _]);
            var specification = (TupleTerm)spec.Items[3];
            var function = (TupleTerm)Cons.Items(specification.Items[1]).Single();
            Term result = Cons.Items(function.Items[3]).Last();

            AssertRange(
                direct,
                Term.I(0),
                Operator(
                    "+",
                    6,
                    37,
                    Operator(
                        "-",
                        6,
                        33,
                        Operator(
                            "bsl",
                            6,
                            26,
                            Term.I(1),
                            Term.I(5)
                        ),
                        Term.I(1)
                    ),
                    Operator(
                        "*",
                        6,
                        41,
                        Term.I(2),
                        Term.I(3)
                    )
                )
            );
            AssertRange(
                expanded,
                Term.I(0),
                Operator(
                    "-",
                    7,
                    30,
                    Operator(
                        "bsl",
                        7,
                        25,
                        Term.I(1),
                        Term.I(32)
                    ),
                    Term.I(1)
                )
            );
            AssertRange(
                result,
                Operator(
                    "-",
                    8,
                    18,
                    Term.I(1)
                ),
                Operator(
                    "-",
                    8,
                    25,
                    Term.I(8),
                    Operator(
                        "*",
                        8,
                        29,
                        Term.I(2),
                        Term.I(3)
                    )
                )
            );
        }
        finally
        {
            temporary.Delete(recursive: true);
        }

        return Task.CompletedTask;
    }

    private static Term Type(IEnumerable<TupleTerm> forms, string name)
    {
        var type = forms.Single(form => form.Items is
            [
                _,
                _,
                Atom { Name: "type" },
                TupleTerm { Items: [Atom { Name: var candidate }, _, _] }
            ] && candidate == name);

        return ((TupleTerm)type.Items[3]).Items[1];
    }

    private static void AssertRange(Term expression, Term expectedLower, Term expectedUpper)
    {
        if (expression is TupleTerm { Items: [Atom { Name: "type" }, _, Atom { Name: "range" }, var bounds] }
            && Cons.Items(bounds).Select(Project).SequenceEqual([expectedLower, expectedUpper]))
        {
            return;
        }

        throw new InvalidOperationException("Type range operators did not use their OTP source annotations.");
    }

    private static Term Project(Term term) => term switch
    {
        TupleTerm { Items: [Atom { Name: "integer" }, _, var value] } => value,
        TupleTerm { Items: var items } when items[0] is Atom { Name: "op" } =>
            Term.Tuple([items[2], items[1], .. items.Skip(3).Select(Project)]),
        _ => throw new InvalidOperationException("Unexpected abstract type operator fixture node.")
    };

    private static Term Operator(
        string symbol,
        int line,
        int column,
        params Term[] operands
    ) => Term.Tuple([Term.A(symbol), Term.Tuple(Term.I(line), Term.I(column)), .. operands]);
}
