-module(beam_proper_list_tail).
-export([from_filter/2, asserted/2, from_input/1, nested_head/2,
         from_sort/1, unique/1, from_local/1, from_reverse/2,
         from_binding/1, from_alias/1, from_existing/2, from_merge/2, from_shadow/2]).
-import(lists, [filter/2, usort/1]).

from_filter(Predicate, Input) ->
    case lists:filter(Predicate, Input) of
        [Value] -> {one, Value};
        _ -> other
    end.

asserted(Predicate, Input) ->
    [Value] = filter(Predicate, Input),
    {one, Value}.

from_input(Input) when is_list(Input) ->
    case Input of
        [Value] -> {one, Value};
        [_|Tail] -> {rest, Tail};
        [] -> empty
    end.

nested_head(Predicate, Input) ->
    case lists:filter(Predicate, Input) of
        [[Value]] -> {nested, Value};
        _ -> other
    end.

from_sort(Input) ->
    case lists:sort(Input) of
        [First, Second] -> {pair, First, Second};
        _ -> other
    end.

unique(Input) ->
    [Value] = usort(Input),
    {only, Value}.

from_local(Input) ->
    case sort(Input) of
        [Value] -> {one, Value};
        _ -> other
    end.

from_reverse(Input, Tail) ->
    case lists:reverse(Input, Tail) of
        [Value] -> {one, Value};
        _ -> other
    end.

sort(Input) -> [Input|improper].

from_binding(Input) ->
    Sorted = lists:sort(Input),
    case Sorted of
        [Value] -> {one, Value};
        _ -> other
    end.

from_alias(Input) ->
    Sorted = lists:sort(Input),
    Alias = Sorted,
    erlang:put(proper_list_alias, seen),
    case Alias of
        [Value] -> {one, Value};
        _ -> other
    end.

from_existing(Input, Saved) ->
    Saved = lists:sort(Input),
    case Saved of
        [Value] -> {one, Value};
        _ -> other
    end.

from_merge(Flag, Input) ->
    Result = case Flag of
        true -> lists:sort(Input);
        false -> [Input|improper]
    end,
    case Result of
        [Value] -> {one, Value};
        _ -> other
    end.

from_shadow(Input, Other) ->
    Result = lists:sort(Input),
    Fun = fun(Result) ->
        case Result of
            [Value] -> {one, Value};
            _ -> other
        end
    end,
    {Result, Fun(Other)}.
