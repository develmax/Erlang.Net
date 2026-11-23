using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamConstructorColumnComprehensionChecks
{
    internal const string TestName = "compiler/beam/lifts-list-helpers-through-mixed-constructors";
    private const string Fixture = "beam_constructor_column_comprehension.erl";
    private const string WrongOrder = "Constructor traversal produced unexpected comprehension helper identities.";
    private const string WrongResult = "Constructor traversal selected a different comprehension.";

    private const string Source = """
        -module(beam_constructor_column_comprehension).
        -export([mixed/2, tuple_shapes/3, promoted_literal/2]).

        mixed(Rows, {z, Tag}) -> [remember(Tag, X) || X <- Rows];
        mixed(Rows, flag) -> [remember(atom, X) || X <- Rows];
        mixed(Rows, []) -> [remember(nil, X) || X <- Rows];
        mixed(Rows, [Tag | _]) -> [remember(Tag, X) || X <- Rows];
        mixed(Rows, {a, Tag}) -> [remember(Tag, X) || X <- Rows];
        mixed(Rows, _) -> [remember(fallback, X) || X <- Rows].

        tuple_shapes(Rows, {z, Tag}, Meta) -> [remember({Tag, Meta}, X) || X <- Rows];
        tuple_shapes(Rows, {a, Tag, Extra}, Meta) -> [remember({Tag, Meta, Extra}, X) || X <- Rows];
        tuple_shapes(Rows, {a, Tag}, Meta) -> [remember({Tag, Meta}, X) || X <- Rows];
        tuple_shapes(Rows, _, Meta) -> [remember({fallback, Meta}, X) || X <- Rows].

        promoted_literal(Rows, [Tag | _]) -> [remember(Tag, X) || X <- Rows];
        promoted_literal(Rows, {closed, value}) -> [remember(literal, X) || X <- Rows];
        promoted_literal(Rows, _) -> [remember(fallback, X) || X <- Rows].

        remember(Tag, X) -> {Tag, X}.
        """;

    internal static async Task Run()
    {
        byte[] bytecode = CodeGeneration.CompileBeamModule(Source, Fixture);
        var functions = ((IEnumerable)Property(Code(bytecode), "Functions")).Cast<object>()
            .OrderBy(function => (int)Property(function, "FirstInstructionIndex"))
            .Select(function => (Name: (string)Property(function, "Name"), Arity: (int)Property(function, "Arity")))
            .ToArray();
        if (!Helpers("mixed", 2).SequenceEqual([
                ("-mixed/2-lc$^5/1-0-", 1),
                ("-mixed/2-lc$^3/1-1-", 2),
                ("-mixed/2-lc$^0/1-2-", 2),
                ("-mixed/2-lc$^4/1-3-", 2),
                ("-mixed/2-lc$^1/1-4-", 1),
                ("-mixed/2-lc$^2/1-5-", 1)
            ])
            || !Helpers("tuple_shapes", 3).SequenceEqual([
                ("-tuple_shapes/3-lc$^3/1-0-", 2),
                ("-tuple_shapes/3-lc$^0/1-1-", 3),
                ("-tuple_shapes/3-lc$^2/1-2-", 3),
                ("-tuple_shapes/3-lc$^1/1-3-", 4)
            ])
            || Helpers("promoted_literal", 2).Any())
        {
            throw new InvalidOperationException(WrongOrder);
        }

        var module = BeamModule.Prepare(bytecode);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term row = Term.A("row");
            Term rows = Term.List(row);
            Term z = Term.A("z");
            Term a = Term.A("a");
            Term tag = Term.A("tag");
            Term meta = Term.A("meta");
            Term extra = Term.A("extra");
            Term fallback = Term.A("fallback");
            (string Function, Term[] Arguments, Term Tag)[] cases =
            [
                ("mixed", [rows, Term.Tuple(z, tag)], tag),
                ("mixed", [rows, Term.A("flag")], Term.A("atom")),
                ("mixed", [rows, Nil.Value], Term.A("nil")),
                ("mixed", [rows, Term.List(tag)], tag),
                ("mixed", [rows, Term.Tuple(a, tag)], tag),
                ("mixed", [rows, Term.I(42)], fallback),
                ("tuple_shapes", [rows, Term.Tuple(z, tag), meta], Term.Tuple(tag, meta)),
                ("tuple_shapes", [rows, Term.Tuple(a, tag, extra), meta], Term.Tuple(tag, meta, extra)),
                ("tuple_shapes", [rows, Term.Tuple(a, tag), meta], Term.Tuple(tag, meta)),
                ("tuple_shapes", [rows, z, meta], Term.Tuple(fallback, meta)),
                ("promoted_literal", [rows, Term.Tuple(Term.A("closed"), Term.A("value"))], Term.A("literal"))
            ];
            foreach (var (function, arguments, expected) in cases)
            {
                if (!Term.List(Term.Tuple(expected, row)).Equals(await module.Invoke(context, function, arguments)))
                {
                    throw new InvalidOperationException(WrongResult + " " + function);
                }
            }

            return Nil.Value;
        });

        IEnumerable<(string Name, int Arity)> Helpers(string function, int arity) => functions
            .Where(helper => helper.Name.StartsWith(FormattableString.Invariant($"-{function}/{arity}-lc$^"), StringComparison.Ordinal));
    }
}
