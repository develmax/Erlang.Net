using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamAtomColumnComprehensionChecks
{
    internal const string TestName = "compiler/beam/lifts-list-helpers-through-grouped-atom-columns";
    private const string Fixture = "beam_atom_column_comprehension.erl";
    private const string WrongOrder = "Atom-column traversal produced unexpected comprehension helpers.";
    private const string WrongEffects = "Atom-column traversal changed selection or evaluation order.";

    private const string Source = """
        -module(beam_atom_column_comprehension).
        -export([choose/3, defaults/3, overlap/3, repeated_binding/3, reset/0, events/0]).

        choose(Rows, z, true) -> [remember(zt, X) || X <- Rows];
        choose(Rows, a, true) -> [remember(at, X) || X <- Rows];
        choose(Rows, z, false) -> [remember(zf, X) || X <- Rows];
        choose(Rows, a, false) -> [remember(af, X) || X <- Rows];
        choose(Rows, _, _) -> [remember(other, X) || X <- Rows].

        defaults(Rows, a, _) -> [remember(atom, X) || X <- Rows];
        defaults(Rows, _, true) -> [remember(yes, X) || X <- Rows];
        defaults(Rows, _, false) -> [remember(no, X) || X <- Rows].

        overlap(Rows, a, _) -> [remember(earlier, X) || X <- Rows];
        overlap(Rows, a, true) -> [remember(later, X) || X <- Rows].

        repeated_binding(Rows, a, a) -> [remember(same_atom, X) || X <- Rows];
        repeated_binding(Rows, V, V) -> [remember(same_term, X) || X <- Rows];
        repeated_binding(Rows, _, _) -> [remember(other, X) || X <- Rows].

        remember(Tag, X) -> erlang:put(events, [{Tag, X} | erlang:get(events)]), X.
        reset() -> erlang:put(events, []), ok.
        events() -> erlang:get(events).
        """;

    internal static async Task Run()
    {
        byte[] bytecode = CodeGeneration.CompileBeamModule(Source, Fixture);
        string[] names = ((IEnumerable)Property(Code(bytecode), "Functions")).Cast<object>()
            .OrderBy(function => (int)Property(function, "FirstInstructionIndex"))
            .Select(function => (string)Property(function, "Name")).ToArray();
        if (!Helpers("choose").SequenceEqual([
                "-choose/3-lc$^4/1-0-",
                "-choose/3-lc$^0/1-1-",
                "-choose/3-lc$^2/1-2-",
                "-choose/3-lc$^1/1-3-",
                "-choose/3-lc$^3/1-4-"
            ])
            || !Helpers("defaults").SequenceEqual([
                "-defaults/3-lc$^1/1-0-",
                "-defaults/3-lc$^2/1-1-",
                "-defaults/3-lc$^0/1-2-"
            ])
            || Helpers("overlap").Any()
            || Helpers("repeated_binding").Any())
        {
            throw new InvalidOperationException(WrongOrder);
        }

        var module = BeamModule.Prepare(bytecode);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term alpha = Term.A("alpha");
            Term beta = Term.A("beta");
            Term rows = Term.List(alpha, beta);
            (string Function, string Left, string Right, string Tag)[] cases =
            [
                ("choose", "z", "true", "zt"),
                ("choose", "a", "false", "af"),
                ("choose", "other", "true", "other"),
                ("defaults", "a", "true", "atom"),
                ("defaults", "other", "true", "yes"),
                ("defaults", "other", "false", "no"),
                ("overlap", "a", "true", "earlier"),
                ("repeated_binding", "a", "a", "same_atom"),
                ("repeated_binding", "b", "b", "same_term"),
                ("repeated_binding", "b", "c", "other")
            ];
            foreach (var (function, left, right, tag) in cases)
            {
                await module.Invoke(context, "reset");
                Term result = await module.Invoke(
                    context,
                    function,
                    rows,
                    Term.A(left),
                    Term.A(right)
                );
                Term effects = Term.List(Term.Tuple(Term.A(tag), beta), Term.Tuple(Term.A(tag), alpha));
                if (!rows.Equals(result) || !effects.Equals(await module.Invoke(context, "events")))
                {
                    throw new InvalidOperationException(WrongEffects + " " + function);
                }
            }

            return Nil.Value;
        });

        IEnumerable<string> Helpers(string function) => names
            .Where(name => name.StartsWith("-" + function + "/3-lc$^", StringComparison.Ordinal));
    }
}
