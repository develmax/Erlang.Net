using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGroupedListGuardChecks
{
    internal const string TestName = "compiler/beam/lifts-list-generators-with-adjacent-guard-filters";
    private const string Fixture = "beam_grouped_generator_guards.erl";
    private const string WrongHelper = "Adjacent guard filters did not share the generator helper.";
    private const string WrongResult = "The generator did not preserve guarded filter behavior.";

    private const string Source = """
        -module(beam_grouped_generator_guards).
        -export([keep/2, values/2, reject/1, filters/1, before/1, after_/1]).
        keep(Items, Min) -> [X || X <- Items, is_integer(X), X >= Min].
        values(Items, Min) -> [N || {N, ok} <- Items, is_integer(N), N >= Min, N =/= 4].
        reject(Items) -> [X || X <- Items, is_integer(X), 12 div X > 2].
        filters(Items) -> [X || X <- Items, custom(X), is_integer(X)].
        before(Items) ->
            {[{X, matched} || X <- Items], [#{value => X} || X <- Items]}.
        after_(Items) ->
            {[#{value => X} || X <- Items], [{X, matched} || X <- Items]}.
        custom(X) -> X =/= bad.
        """;

    internal static async Task Run()
    {
        byte[] bytecode = CodeGeneration.CompileBeamModule(Source, Fixture);
        var helpers = ((IEnumerable)Property(Code(bytecode), "Functions")).Cast<object>()
            .Select(function => ((string)Property(function, "Name"), (int)Property(function, "Arity")))
            .ToHashSet();
        if (!helpers.Contains(("-keep/2-lc$^0/1-0-", 2))
            || !helpers.Contains(("-values/2-lc$^0/1-0-", 2))
            || !helpers.Contains(("-reject/1-lc$^0/1-0-", 1))
            || !helpers.Contains(("-before/1-lc$^1/1-0-", 1))
            || !helpers.Contains(("-after_/1-lc$^0/1-1-", 1))
            || !helpers.Contains(("-filters/1-lc$^0/1-0-", 1)))
        {
            throw new InvalidOperationException(WrongHelper);
        }

        var module = BeamModule.Prepare(bytecode);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term zero = Term.I(0);
            Term one = Term.I(1);
            Term two = Term.I(2);
            Term four = Term.I(4);
            Term six = Term.I(6);
            Term ok = Term.A("ok");
            Term bad = Term.A("bad");
            Term items = Term.List(
                zero,
                one,
                bad,
                four,
                six,
                two
            );
            Term tuples = Term.List(
                Term.Tuple(zero, ok),
                Term.Tuple(two, ok),
                Term.Tuple(four, ok),
                Term.Tuple(six, bad),
                bad
            );
            Term matched = Term.List(Term.Tuple(two, Term.A("matched")));
            Term mapped = Term.List(new MapTerm([new(Term.A("value"), two)]));
            if (!Term.Tuple(matched, mapped).Equals(await module.Invoke(context, "before", Term.List(two)))
                || !Term.Tuple(mapped, matched).Equals(await module.Invoke(context, "after_", Term.List(two)))
                || !Term.List(four, six, two).Equals(await module.Invoke(
                    context,
                    "keep",
                    items,
                    two
                ))
                || !Term.List(two).Equals(await module.Invoke(
                    context,
                    "values",
                    tuples,
                    two
                ))
                || !Term.List(one, four, two).Equals(await module.Invoke(context, "reject", items))
                || !Term.List(
                    zero,
                    one,
                    four,
                    six,
                    two
                ).Equals(await module.Invoke(context, "filters", items))
                || !Nil.Value.Equals(await module.Invoke(
                    context,
                    "keep",
                    Nil.Value,
                    two
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;
        });
    }
}
