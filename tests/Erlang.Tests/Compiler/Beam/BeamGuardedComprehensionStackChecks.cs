using System.Collections;
using Erlang;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedComprehensionStackChecks
{
    internal const string TestName = "compiler/beam/keeps-guarded-comprehension-failure-frames";
    private const string Fixture = "beam_guarded_comprehension_stack_probe.erl";
    private const string Entry = "entry";
    private const string Helper = "-run/2-lc$^0/1-0-";
    private const string IncorrectValue = "The guarded comprehension changed a decoded result.";
    private const string IncorrectStack = "The guarded comprehension lost its helper frame or source line.";

    private const string Source = """
        -module(beam_guarded_comprehension_stack_probe).
        -export([entry/1, if_values/2, if_partial/1, if_pair/3, bit_pair/3, bit_width/2, tuple_case/2, tuple_block/2]).

        entry(Input) -> run(Input, []).

        run(Input, Tags) ->
            Items = prepare(Input, Tags),
            [begin
                Value = decode(Item, [2]),
                if
                    0 =< Value, Value =< 9 -> Value;
                    true -> exit({error, bad_range})
                end
            end || Item <- Items].

        prepare(Input, []) -> Input.
        decode(Item, [2]) -> Item.

        if_values(Input, Limit) ->
            [if is_integer(Item), Item < Limit -> {small,Item};
                true -> Item
             end || Item <- Input].

        if_partial(Input) ->
            [if is_atom(Item) -> Item end || Item <- Input].

        if_pair(First, Second, Limit) ->
            A = [if Item < Limit -> Item; true -> Limit end || Item <- First],
            B = [if is_atom(Item) -> {Item,Limit}; true -> Item end || Item <- Second],
            {A,B}.

        bit_pair(Mode, Values, Bound) ->
            if Mode =:= first ->
                    [begin
                         Current = Item,
                         if Current =:= Bound -> <<0:8>>;
                            true -> <<1:2,Current:6>>
                         end
                     end || Item <- Values];
               Mode =:= second ->
                    [if Item =:= Bound -> <<9:8>>;
                        true -> <<0:8,Item:8>>
                     end || Item <- Values];
               true ->
                    fun(Item) -> <<Item:8>> end
            end.

        bit_width(Values, Width) ->
            [if is_integer(Item) -> <<Item:Width>>; true -> <<>> end || Item <- Values].

        tuple_case(Values, Limit) ->
            [{Kind, case Kind of keep -> Value; _ -> Limit end}
             || {Kind,Value} <- Values].

        tuple_block(Values, Expected) ->
            [begin
                 Expected = Key,
                 {Value,Expected}
             end || {Key,Value} <- Values].
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        var functions = ((IEnumerable)Property(Code(image), "Functions")).Cast<object>()
            .Select(function => ((string)Property(function, "Name"), (int)Property(function, "Arity"))).ToArray();
        if (!functions.Contains(("-if_values/2-lc$^0/1-0-", 2))
            || !functions.Contains(("-if_partial/1-lc$^0/1-0-", 1))
            || !functions.Contains(("-if_pair/3-lc$^0/1-0-", 2))
            || !functions.Contains(("-if_pair/3-lc$^1/1-1-", 2))
            || !functions.Contains(("-bit_pair/3-fun-2-", 1))
            || !functions.Contains(("-bit_pair/3-lc$^0/1-0-", 2))
            || !functions.Contains(("-bit_pair/3-lc$^1/1-1-", 2))
            || functions.Any(function => function.Item1.StartsWith("-bit_width/2-lc$^", StringComparison.Ordinal))
            || !functions.Contains(("-tuple_case/2-lc$^0/1-0-", 2))
            || !functions.Contains(("-tuple_block/2-lc$^0/1-0-", 2)))
        {
            throw new InvalidOperationException(IncorrectValue);
        }

        var module = BeamModule.Prepare(image, new BeamDependencySelection(["lists"]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term input = Term.List(Term.I(0), Term.I(9));
            if (!input.Equals(await module.Invoke(context, Entry, input))
                || !Nil.Value.Equals(await module.Invoke(context, Entry, Nil.Value)))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            try
            {
                await module.Invoke(context, Entry, Term.List(Term.I(0), Term.I(10)));
                throw new InvalidOperationException(IncorrectValue);
            }
            catch (ErlangException error)
            {
                RequireFrame(error, Term.Tuple(Term.A("error"), Term.A("bad_range")), ErrorLine("true -> exit"));
            }

            Term invalidSource = Term.A("not_a_list");
            try
            {
                await module.Invoke(context, Entry, invalidSource);
                throw new InvalidOperationException(IncorrectValue);
            }
            catch (ErlangException error)
            {
                RequireFrame(error, Term.Tuple(Term.A("bad_generator"), invalidSource), ErrorLine("Item <- Items"));
            }

            Term small = Term.I(1);
            Term limit = Term.I(3);
            Term atom = Term.A("other");
            Term values = await module.Invoke(
                context,
                "if_values",
                Term.List(small, limit, atom),
                limit
            );
            Term pair = await module.Invoke(
                context,
                "if_pair",
                Term.List(small, limit),
                Term.List(atom),
                limit
            );
            if (!Term.List(Term.Tuple(Term.A("small"), small), limit, atom).Equals(values)
                || !Term.Tuple(Term.List(small, limit), Term.List(Term.Tuple(atom, limit))).Equals(pair)
                || !Nil.Value.Equals(await module.Invoke(context, "if_partial", Nil.Value)))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            try
            {
                await module.Invoke(context, "if_partial", Term.List(atom, small));
                throw new InvalidOperationException(IncorrectValue);
            }
            catch (ErlangException error)
            {
                if (!Term.A("if_clause").Equals(error.Reason)
                    || error.StackTraceTerm is not
                    Cons
                    {
                        Head: TupleTerm { Items: [_, Atom { Name: "-if_partial/1-lc$^0/1-0-" }, _, _] }
                    })
                {
                    throw new InvalidOperationException(IncorrectStack);
                }
            }

            Term firstBits = await module.Invoke(
                context,
                "bit_pair",
                Term.A("first"),
                Term.List(small, limit),
                limit
            );
            Term secondBits = await module.Invoke(
                context,
                "bit_pair",
                Term.A("second"),
                Term.List(small, limit),
                limit
            );
            Term dynamicBits = await module.Invoke(
                context,
                "bit_width",
                Term.List(small),
                Term.I(4)
            );
            if (!Term.List(new BitString([65]), new BitString([0])).Equals(firstBits)
                || !Term.List(new BitString([0, 1]), new BitString([9])).Equals(secondBits)
                || !Term.List(new BitString([16], 4)).Equals(dynamicBits))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            try
            {
                await module.Invoke(
                    context,
                    "bit_pair",
                    Term.A("first"),
                    Term.List(atom, small),
                    limit
                );
                throw new InvalidOperationException(IncorrectValue);
            }
            catch (ErlangException error)
            {
                if (!Term.A("badarg").Equals(error.Reason)
                    || error.StackTraceTerm is not Cons
                    {
                        Head: TupleTerm { Items: [_, Atom { Name: "-bit_pair/3-lc$^0/1-0-" }, _, var location] }
                    }
                    || !Cons.Items(location).Contains(Term.Tuple(
                        Term.A("line"),
                        Term.I(ErrorLine("true -> <<1:2,Current:6>>"))
                    )))
                {
                    throw new InvalidOperationException(IncorrectStack);
                }
            }

            Term keep = Term.A("keep");
            Term change = Term.A("change");
            Term tupleItems = Term.List(Term.Tuple(keep, small), atom, Term.Tuple(change, small));
            Term cases = await module.Invoke(
                context,
                "tuple_case",
                tupleItems,
                limit
            );
            Term matched = await module.Invoke(
                context,
                "tuple_block",
                Term.List(atom, Term.Tuple(keep, small)),
                keep
            );
            if (!Term.List(Term.Tuple(keep, small), Term.Tuple(change, limit)).Equals(cases)
                || !Term.List(Term.Tuple(small, keep)).Equals(matched))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            try
            {
                await module.Invoke(
                    context,
                    "tuple_block",
                    tupleItems,
                    keep
                );
                throw new InvalidOperationException(IncorrectValue);
            }
            catch (ErlangException error)
            {
                if (!Term.Tuple(Term.A("badmatch"), change).Equals(error.Reason)
                    || error.StackTraceTerm is not Cons
                    {
                        Head: TupleTerm { Items: [_, Atom { Name: "-tuple_block/2-lc$^0/1-0-" }, _, _] }
                    })
                {
                    throw new InvalidOperationException(IncorrectStack);
                }
            }

            return Nil.Value;
        });
    }

    private static int ErrorLine(string anchor) =>
        Source.Split('\n').Select((line, index) => (line, index)).Single(
            item => item.line.Contains(anchor, StringComparison.Ordinal)
        ).index + 1;

    private static void RequireFrame(ErlangException error, Term reason, int line)
    {
        if (!reason.Equals(error.Reason)
            || error.StackTraceTerm is not Cons { Head: TupleTerm { Items: [_, Atom name, Integer arity, var location] } }
            || name.Name != Helper || arity.Value != 1
            || !Cons.Items(location).Any(value => value.Equals(Term.Tuple(Term.A("line"), Term.I(line)))))
        {
            throw new InvalidOperationException(IncorrectStack);
        }
    }
}
