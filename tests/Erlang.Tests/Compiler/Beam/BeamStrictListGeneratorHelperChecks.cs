using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamStrictListGeneratorHelperChecks
{
    internal const string TestName = "compiler/beam/lifts-single-strict-list-generators";
    private const string Fixture = "beam_strict_list_generator_helper.erl";
    private const string StrictHelper = "-strict/1-lc$^0/1-0-";
    private const string SplitHelper = "-split_source/1-lc$^0/1-0-";
    private const string WrongHelpers = "Strict helper identities or nested scope boundaries differed.";
    private const string WrongResult = "Strict list traversal, failure reason, source line, or effect order differed.";

    private const string Source = """
        -module(beam_strict_list_generator_helper).
        -export([strict/1, relaxed/1, variable/1, nested/1, observe/1, split_source/1]).
        strict(Input) ->
            [V || {tag,V} <:- Input].
        relaxed(Input) ->
            [V || {tag,V} <- Input].
        variable(Input) ->
            [V || V <:- Input].
        nested(Input) ->
            [[V || {tag,V} <:- Row] || Row <- Input].
        observe(Input) ->
            erase(events),
            try strict_effect(Input) of
                Value -> {Value, get(events)}
            catch error:Reason ->
                {Reason, get(events)}
            end.
        strict_effect(Input) ->
            [remember(V) || {tag,V} <:- Input].
        remember(V) ->
            Before = case get(events) of undefined -> []; Other -> Other end,
            put(events, [V | Before]),
            V.
        split_source(Input) ->
            [V || {tag,V} <:-
                  select_input(Input)].
        select_input(Input) -> Input.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        var functions = ((IEnumerable)Property(Code(beam), "Functions")).Cast<object>()
            .Select(function => (string)Property(function, "Name")).ToHashSet(StringComparer.Ordinal);
        if (!functions.Contains(StrictHelper) || !functions.Contains(SplitHelper)
            || !functions.Contains("-variable/1-lc$^0/1-0-")
            || functions.Any(function => function.StartsWith("-nested/1-lc$^", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(WrongHelpers);
        }

        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term one = Term.I(1);
            Term two = Term.I(2);
            Term bad = Term.A("bad");
            Term invalidTail = Term.A("invalid_tail");
            Term first = Term.Tuple(Term.A("tag"), one);
            Term second = Term.Tuple(Term.A("tag"), two);
            Term both = Term.List(first, second);
            Term interrupted = Term.List(first, bad, second);
            Term values = Term.List(one, two);
            Term mismatch = Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), bad);
            Term invalidGenerator = Term.Tuple(Term.A(ErlangErrorReasons.BadGenerator), invalidTail);

            RequireEqual(values, await module.Invoke(context, "strict", both));
            RequireEqual(Nil.Value, await module.Invoke(context, "strict", Nil.Value));
            RequireEqual(values, await module.Invoke(context, "relaxed", interrupted));
            RequireEqual(interrupted, await module.Invoke(context, "variable", interrupted));
            RequireEqual(Term.List(Nil.Value, values), await module.Invoke(context, "nested", Term.List(Nil.Value, both)));
            RequireEqual(Term.Tuple(values, Term.List(two, one)), await module.Invoke(context, "observe", both));
            RequireEqual(Term.Tuple(mismatch, Term.List(one)), await module.Invoke(context, "observe", interrupted));

            await CheckFailure(
                "strict",
                interrupted,
                mismatch,
                StrictHelper,
                SourceLine("<:- Input].")
            );
            await CheckFailure(
                "strict",
                new Cons(first, invalidTail),
                invalidGenerator,
                StrictHelper,
                SourceLine("<:- Input].")
            );
            await CheckFailure(
                "split_source",
                invalidTail,
                invalidGenerator,
                SplitHelper,
                SourceLine("select_input(Input)].")
            );
            await CheckFailure("nested", Term.List(interrupted), mismatch);

            return Nil.Value;

            async ValueTask CheckFailure(
                string function,
                Term input,
                Term reason,
                string? helper = null,
                int line = 0
            )
            {
                try
                {
                    await module.Invoke(context, function, input);
                }
                catch (ErlangException error) when (reason.Equals(error.Reason))
                {
                    if (helper is null)
                    {
                        return;
                    }

                    if (error.StackTraceTerm is Cons { Head: TupleTerm { Items: [_, Atom name, _, var location] } }
                        && name.Name == helper
                        && Cons.Items(location).Any(item => item.Equals(Term.Tuple(Term.A("line"), Term.I(line)))))
                    {
                        return;
                    }
                }

                throw new InvalidOperationException(WrongResult + " " + function);
            }
        });
    }

    private static int SourceLine(string fragment) =>
        Array.FindIndex(Source.Split('\n'), line => line.Contains(fragment, StringComparison.Ordinal)) + 1;

    private static void RequireEqual(Term expected, Term actual)
    {
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException(WrongResult + " " + actual);
        }
    }
}
