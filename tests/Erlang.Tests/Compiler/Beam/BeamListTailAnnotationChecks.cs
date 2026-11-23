using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamListTailAnnotationChecks
{
    internal const string TestName = "compiler/beam/list-tail-keeps-first-source-annotation";
    private const string Module = "beam_list_tail_anno";
    private const string Mismatch = "Expected list tails to keep Erlang parser positions for macro, grouped, and explicit tail elements.";

    internal static Task Run()
    {
        const string source = """
            -module(beam_list_tail_anno).
            -export([probe/1, control/2, ordinary/3]).
            -define(PART(M,F,A,N), {open,N},{field,_,M},{separator,_},{field,_,F},{separator,_},{size,_,A},{close,_}).
            probe([{'{',N},?PART(M,F,A,_),{'}',_}]) -> {N,M,F,A}.
            control(A,B) -> [A | [B]].
            ordinary(A,B,C) -> [A,(B),C].
            """;
        var directory = Directory.CreateTempSubdirectory("erlang-list-tail-annotation-");
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
            Verify(CodeGeneration.CompileBeamFile(path, buildProfile: profile));
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
            throw new InvalidOperationException(Mismatch);
        }

        var functions = Cons.Items(forms).OfType<TupleTerm>()
            .Where(form => form.Items is [Atom { Name: "function" }, _, _, _, _])
            .ToDictionary(form => ((Atom)form.Items[2]).Name, form => (TupleTerm)Cons.Items(form.Items[4]).Single());
        Check(
            Cons.Items(functions["probe"].Items[2]).Single(),
            4,
            [(7, 8), (17, 17), (22, 28), (22, 22), (22, 22), (24, 24), (24, 24), (26, 26), (31, 31)]
        );
        Check(Cons.Items(functions["control"].Items[4]).Single(), 5, [(17, 18), (22, 23)]);
        Check(Cons.Items(functions["ordinary"].Items[4]).Single(), 6, [(20, 21), (24, 24), (27, 27)]);
    }

    private static void Check(Term list, int line, (int Cons, int Head)[] expected)
    {
        foreach (var (cons, head) in expected)
        {
            if (list is not TupleTerm
                {
                    Items: [Atom { Name: "cons" }, var start, TupleTerm value, var tail]
                }
                || !start.Equals(Term.Tuple(Term.I(line), Term.I(cons)))
                || !value.Items[1].Equals(Term.Tuple(Term.I(line), Term.I(head))))
            {
                throw new InvalidOperationException(Mismatch);
            }

            list = tail;
        }

        if (list is not TupleTerm { Items: [Atom { Name: "nil" }, _] })
        {
            throw new InvalidOperationException(Mismatch);
        }
    }
}
