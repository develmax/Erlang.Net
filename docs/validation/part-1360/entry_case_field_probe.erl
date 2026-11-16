-module(entry_case_field_probe).
-export([record_lookup/3, tuple_lookup/2, private_lookup/2, guarded_lookup/3, shadow_lookup/2]).
-record(ctx, {scope, namespace}).

record_lookup(Key, Local, #ctx{namespace = Names}) ->
    case lists:keysearch(Key, 1, Names) of
        {value,{_,Uri}} -> {Uri,list_to_atom(Local)};
        false -> []
    end.

tuple_lookup(Key, {pair, Names}) ->
    case maps:find(Key, Names) of
        {ok,Value} -> {found,Value};
        _ -> missing
    end.

private_lookup(Key, Names) ->
    internal(Key, #ctx{scope = private, namespace = Names}).

internal(Key, #ctx{namespace = Names}) ->
    case maps:find(Key, Names) of
        {ok,Value} -> {found,Value};
        _ -> missing
    end.

guarded_lookup(Mode, {pair, Field}, Fallback) when is_atom(Mode) ->
    case find(Mode, Field) of
        good -> yes;
        _ -> Fallback
    end.

find(Mode, Value) ->
    erlang:put(last_value, Value),
    Mode.

shadow_lookup(Pair = {pair, Names}, Other) ->
    F = fun(Pair) -> checked(Pair) end,
    {Names, F(Other)}.

checked({pair, Names}) ->
    case maps:find(key, Names) of
        {ok,Value} -> {found,Value};
        _ -> missing
    end.

