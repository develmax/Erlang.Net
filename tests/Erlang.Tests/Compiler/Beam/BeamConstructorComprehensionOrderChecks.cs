using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamConstructorComprehensionOrderChecks
{
    internal const string TestName = "compiler/beam/constructor-comprehension-order";

    private const string Source = """
        -module(beam_constructor_comprehension_order).
        -export([cons/3, record/2, mixed/2, siblings/2, shadow/2, mixed_map/2]).
        -record(box, {kind, values}).
        -record(wide, {tag, key, values}).

        cons(Tuple, Fields, Hidden) when is_tuple(Tuple) ->
            cons(tuple_to_list(Tuple), Fields, Hidden);
        cons([_Name|Values], Fields, Hidden) ->
            Width = case Fields of
                [] -> 0;
                _ -> lists:max([erlang:atom_to_binary(Field) || Field <- Fields])
            end,
            {[{Width,Tag,Value} || {Tag,Value} <- lists:zip(Fields,Values), Tag =/= Hidden], Width}.

        record(#box{kind={Type,Name}, values=Values}=Box, Extra) ->
            Rendered = [target:render({Type,Name,Item}) || Item <- Values],
            {Rendered, Box, Extra};
        record(#box{values=Values}, Extra) ->
            {Values,Extra};
        record(Wide, Extra) when is_record(Wide, wide) ->
            Kind = Wide#wide.tag,
            Input = Wide#wide.key,
            Existing = target:marker(),
            case Kind of
                A when is_atom(A) -> [target:render({A,Item}) || Item <- Input];
                {other, Existing} -> already_seen;
                {other, Key} -> [target:render({Key,Item,Extra}) || Item <- Input];
                _ -> []
            end.

        mixed(X, Values) ->
            case X of
                A when is_atom(A) -> [target:render({a,Item}) || Item <- Values];
                {T} -> [target:render({T,Item}) || Item <- Values];
                _ -> [target:render({default,Item}) || Item <- Values]
            end.

        siblings(A, Values) ->
            case A of
                [_] -> [target:render({a,Item}) || Item <- Values];
                {b} -> [target:render({b,Item}) || Item <- Values];
                [] -> [target:render({empty,Item}) || Item <- Values];
                _ -> [target:render({default,Item}) || Item <- Values]
            end.

        shadow(A, Xs) ->
            case A of
                {B,B} -> [target:render({B,X}) || X <- Xs];
                _ -> [target:render({good,X}) || X <- Xs]
            end.

        mixed_map(A, Values) ->
            case A of
                {a} -> {[target:render(X) || X <- Values], #{X => X || X <- Values}};
                _ -> [target:render(X) || X <- Values]
            end.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_constructor_comprehension_order.erl"));
        var functions = ((IEnumerable)Property(code, "Functions")).Cast<object>()
            .Select(function => ((string)Property(function, "Name"), (int)Property(function, "Arity"))).ToArray();

        Check("cons", [("-cons/3-lc$^1/1-1-", 3), ("-cons/3-lc$^0/1-0-", 1)]);
        Check("record", [("-record/2-lc$^0/1-0-", 3), ("-record/2-lc$^2/1-1-", 3), ("-record/2-lc$^1/1-2-", 2)]);
        Check("mixed", [("-mixed/2-lc$^2/1-0-", 1), ("-mixed/2-lc$^1/1-1-", 2), ("-mixed/2-lc$^0/1-2-", 1)]);
        Check(
            "siblings",
            [
                ("-siblings/2-lc$^3/1-0-", 1),
                ("-siblings/2-lc$^1/1-1-", 1),
                ("-siblings/2-lc$^0/1-2-", 1),
                ("-siblings/2-lc$^2/1-3-", 1)
            ]
        );
        Check("shadow", [("-shadow/2-lc$^1/1-0-", 1), ("-shadow/2-lc$^0/1-1-", 2)]);
        Check("mixed_map", []);

        void Check(string owner, (string, int)[] expected)
        {
            var actual = functions.Where(function => function.Item1.StartsWith($"-{owner}/", StringComparison.Ordinal));
            if (!actual.SequenceEqual(expected))
            {
                throw new InvalidOperationException($"Unexpected comprehension function order in {owner}.");
            }
        }
    }
}
