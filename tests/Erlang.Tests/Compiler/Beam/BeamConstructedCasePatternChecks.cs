using System.Collections;
using Erlang;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamConstructedCasePatternChecks
{
    internal const string TestName = "compiler/beam/preserves-pattern-shape-for-a-constructed-case-selector";
    internal const string Fixture = "beam_constructed_case_pattern_probe.erl";
    private const string ChangedLiteralSelection = "The constructor case introduced a compound pattern literal or the opaque selector lost one.";

    internal const string Source = """
        -module(beam_constructed_case_pattern_probe).
        -export([constructed/5, opaque/1, load/1, carry/5]).

        constructed(First, Second, Third, Fourth, Fifth) ->
            {ok, Left} = ?MODULE:load(First),
            {ok, Right} = ?MODULE:load(Fifth),
            case {see(Left), see(Right)} of
                {true, true} -> ?MODULE:carry(Left, Right, Second, Third, Fourth);
                _ -> neither
            end.

        opaque(A) ->
            case ?MODULE:load(A) of
                {ok, opaque} -> both;
                _ -> neither
            end.

        load(A) -> {ok, A}.
        carry(A, B, C, D, E) -> {A, B, C, D, E}.
        see(A) ->
            Previous = get(constructed_case_effects),
            put(constructed_case_effects, {A,Previous}),
            A.
        """;

    internal static Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object structure = Property(Code(image), "Structure");
        Term[] literals = ((IEnumerable)Property(structure, "Literals")).Cast<Term>().ToArray();
        if (literals.Contains(Term.Tuple(Term.A("true"), Term.A("true")))
            || !literals.Contains(Term.Tuple(Term.A("ok"), Term.A("opaque"))))
        {
            throw new InvalidOperationException(ChangedLiteralSelection);
        }

        return Task.CompletedTask;
    }
}
