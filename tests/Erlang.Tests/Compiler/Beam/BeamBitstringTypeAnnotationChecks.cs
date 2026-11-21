using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamBitstringTypeAnnotationChecks
{
    internal const string TestName = "compiler/beam/bitstring-type-default-annotations";
    private const string Module = "beam_bitstring_type_annotations";
    private const string Failure = "Bitstring type defaults must retain only the opening line and explicit operands must retain their columns.";

    internal static Task Run()
    {
        const string source = """
            -module(beam_bitstring_type_annotations).
            -export([identity/1]).
            -export_type([empty/0, base/0, unit/0, both/0, ordinary/0, remapped/0]).
            -type empty() :: <<>>.
            -type base() :: <<_:0>>.
            -type unit() :: <<_:_*0>>.
            -type both() :: <<_:0,_:_*0>>.
            -type ordinary() :: 0.
            -spec identity(<<_:_*8>>) -> <<_:0>>.
            identity(X) -> X.
            -file("type_alias.erl", 101).
            -type remapped() :: <<>>.
            """;
        var directory = Directory.CreateTempSubdirectory("erlang-bitstring-types-");
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
            byte[] beam = CodeGeneration.CompileBeamFile(path, buildProfile: profile);
            Verify(beam);
        }
        finally
        {
            directory.Delete(recursive: true);
        }

        return Task.CompletedTask;
    }

    private static void Verify(byte[] beam)
    {
        byte[] debug = Chunks(beam).Single(chunk => chunk.Id == "Dbgi").Data;
        if (ExternalTermFormat.Decode(RawEtf(debug)) is not TupleTerm
            {
                Items: [_, _, TupleTerm { Items: [var forms, _] }]
            })
        {
            throw new InvalidOperationException(Failure);
        }

        var attributes = Cons.Items(forms).OfType<TupleTerm>().Where(form =>
            form.Items is [Atom { Name: "attribute" }, _, _, _]).ToArray();
        var types = attributes.Where(form => form.Items[2] is Atom { Name: "type" })
            .Select(form => (TupleTerm)form.Items[3]).ToDictionary(form => ((Atom)form.Items[0]).Name, form => form.Items[1]);

        if (!types["empty"].Equals(Bitstring(
            4,
            18,
            Synthesized(4),
            Synthesized(4)
        ))
            || !types["base"].Equals(Bitstring(
                5,
                17,
                IntegerAt(5, 21),
                Synthesized(5)
            ))
            || !types["unit"].Equals(Bitstring(
                6,
                17,
                Synthesized(6),
                IntegerAt(6, 23)
            ))
            || !types["both"].Equals(Bitstring(
                7,
                17,
                IntegerAt(7, 21),
                IntegerAt(7, 27)
            ))
            || !types["ordinary"].Equals(IntegerAt(8, 21))
            || !types["remapped"].Equals(Bitstring(
                102,
                21,
                Synthesized(102),
                Synthesized(102)
            )))
        {
            throw new InvalidOperationException(Failure);
        }

        var spec = (TupleTerm)attributes.Single(form => form.Items[2] is Atom { Name: "spec" }).Items[3];
        var function = (TupleTerm)Cons.Items(spec.Items[1]).Single();
        if (Cons.Items(function.Items[3]).ToArray() is not [TupleTerm product, var result]
            || !Cons.Items(product.Items[3]).Single().Equals(Bitstring(
                9,
                16,
                Synthesized(9),
                IntegerAt(9, 22, 8)
            ))
            || !result.Equals(Bitstring(
                9,
                30,
                IntegerAt(9, 34),
                Synthesized(9)
            )))
        {
            throw new InvalidOperationException(Failure);
        }
    }

    private static Term Synthesized(int line) => Term.Tuple(Term.A("integer"), Term.I(line), Term.I(0));

    private static Term IntegerAt(int line, int column, int value = 0) =>
        Term.Tuple(Term.A("integer"), Term.Tuple(Term.I(line), Term.I(column)), Term.I(value));

    private static Term Bitstring(
        int line,
        int column,
        Term basis,
        Term unit
    ) => Term.Tuple(
        Term.A("type"),
        Term.Tuple(Term.I(line), Term.I(column)),
        Term.A("binary"),
        Term.List(basis, unit)
    );
}
