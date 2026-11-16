-module(two_call_case_probe).
-export([rounded/1, rounded_list/2, bound/1, nested/1, preserved/1, disjoint/1, rebound/1, shadowed/1]).
-compile({no_auto_import,[round/1]}).

rounded(Value) ->
    Input = read(value, Value),
    case trunc(Input) of
        Integer when Integer < Input -> {number, Integer+1};
        Integer -> {number, Integer}
    end.

rounded_list(Key, [Value]) ->
    Input = read(Key, Value),
    case erlang:floor(Input) of
        Integer when Integer < Input -> {number, Integer+1};
        Integer -> {number, Integer}
    end.

bound(Value) ->
    Selected = read(bound, Value),
    case Selected of
        ready -> accepted;
        _ -> fallback
    end.

nested(Value) ->
    Selected = read(nested, Value),
    case lists:member(Selected, [a,b]) of
        true -> accepted;
        false -> fallback
    end.

rebound(Value) ->
    Value = read(rebound, ready),
    case Value of
        ready -> accepted;
        _ -> fallback
    end.

shadowed(Value) ->
    Input = read(shadowed, Value),
    case round(Input) of
        Integer when Integer < Input -> {number, Integer+1};
        Integer -> {number, Integer}
    end.

round(_) ->
    0.

read(Key, Value) ->
    erlang:put(Key, Value),
    Value.

preserved(Value) ->
    Selected = read(preserved, Value),
    case lists:member(Selected, [a,b]) of
        true -> {accepted, Selected};
        false -> {fallback, Selected}
    end.

disjoint(Value) ->
    Selected = read(disjoint, Value),
    case lists:member(Value, [a,b]) of
        true -> {accepted, Selected};
        false -> {fallback, Selected}
    end.

