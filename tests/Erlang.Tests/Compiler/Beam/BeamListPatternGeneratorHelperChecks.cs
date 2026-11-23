using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamListPatternGeneratorHelperChecks
{
    internal const string TestName = "compiler/beam/lifts-relaxed-list-pattern-generators";
    private const string Fixture = "beam_list_pattern_generator_helper.erl";
    private const string WrongTopology = "List generator lowering produced an unexpected helper topology.";
    private const string WrongResult = "List generator lowering changed the result, error, or effects.";

    private const string Source = """
        -module(beam_list_pattern_generator_helper).
        -export([single/1, head/1, captured/2, strict/1, reset/0, events/0]).
        single(Rows) -> [remember(X) || [X] <- Rows].
        head(Rows) -> [remember(X) || [X | _] <- Rows].
        captured(Rows, Tag) -> [{Tag, remember(X)} || [X] <- Rows].
        strict(Rows) -> [remember(X) || [X] <:- Rows].
        remember(X) -> erlang:put(events, [X | erlang:get(events)]), X.
        reset() -> erlang:put(events, []), ok.
        events() -> erlang:get(events).
        """;

    internal static async Task Run()
    {
        byte[] bytecode = CodeGeneration.CompileBeamModule(Source, Fixture);
        var functions = ((IEnumerable)Property(Code(bytecode), "Functions")).Cast<object>()
            .Select(function => (Name: (string)Property(function, "Name"), Arity: (int)Property(function, "Arity")))
            .ToHashSet();
        if (!functions.Contains(("-single/1-lc$^0/1-0-", 1))
            || !functions.Contains(("-head/1-lc$^0/1-0-", 1))
            || !functions.Contains(("-captured/2-lc$^0/1-0-", 2))
            || functions.Any(function => function.Name.StartsWith("-strict/1-lc$^", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(WrongTopology);
        }

        var module = BeamModule.Prepare(bytecode);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term alpha = Term.A("alpha");
            Term beta = Term.A("beta");
            Term tag = Term.A("tag");
            Term bad = Term.A("bad");
            Term singleton = Term.List(alpha);
            Term pair = Term.List(beta, tag);
            Term improper = new Cons(tag, bad);
            Term rows = Term.List(
                Nil.Value,
                singleton,
                bad,
                pair,
                improper
            );

            await module.Invoke(context, "reset");
            Require(singleton, await module.Invoke(context, "single", rows));
            Require(singleton, await module.Invoke(context, "events"));

            await module.Invoke(context, "reset");
            Require(Term.List(alpha, beta, tag), await module.Invoke(context, "head", rows));
            Require(Term.List(tag, beta, alpha), await module.Invoke(context, "events"));

            await module.Invoke(context, "reset");
            Require(Term.List(Term.Tuple(tag, alpha)), await module.Invoke(
                context,
                "captured",
                rows,
                tag
            ));
            Require(singleton, await module.Invoke(context, "events"));

            Term badGenerator = Term.Tuple(Term.A(ErlangErrorReasons.BadGenerator), bad);
            Term badMatch = Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), pair);
            await RequireError(
                "single",
                bad,
                badGenerator,
                Nil.Value,
                "-single/1-lc$^0/1-0-"
            );
            await RequireError(
                "head",
                new Cons(singleton, bad),
                badGenerator,
                singleton,
                "-head/1-lc$^0/1-0-"
            );
            await RequireError(
                "strict",
                Term.List(singleton, pair, Term.List(beta)),
                badMatch,
                singleton,
                helper: null
            );

            return Nil.Value;

            async ValueTask RequireError(
                string function,
                Term input,
                Term reason,
                Term effects,
                string? helper
            )
            {
                await module.Invoke(context, "reset");
                try
                {
                    await module.Invoke(context, function, input);
                }
                catch (ErlangException error) when (reason.Equals(error.Reason))
                {
                    if (helper is not null
                        && (error.StackTraceTerm is not Cons
                        {
                            Head: TupleTerm { Items: [_, Atom { Name: var name }, _, _] }
                        } || name != helper))
                    {
                        throw new InvalidOperationException(WrongResult);
                    }

                    Require(effects, await module.Invoke(context, "events"));

                    return;
                }

                throw new InvalidOperationException(WrongResult);
            }
        });
    }

    private static void Require(Term expected, Term actual)
    {
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException(WrongResult);
        }
    }
}
