-module(binding_liveness_probe).
-export([arithmetic/2, tuple_value/2, list_value/2, alias_live/2]).
arithmetic(Key, Keep) ->
    Old = erlang:put(Key, Keep),
    Next = Old + 1,
    erlang:put(observed, Next),
    {Next, Keep}.
tuple_value(Key, Keep) ->
    Old = erlang:put(Key, Keep),
    Next = {Old, Keep},
    erlang:put(observed, Next),
    {Next, Keep}.
list_value(Key, Keep) ->
    Old = erlang:put(Key, Keep),
    Next = [Old | Keep],
    erlang:put(observed, Next),
    {Next, Keep}.
alias_live(Key, Keep) ->
    Old = erlang:put(Key, Keep),
    Alias = Old,
    Next = Old + 1,
    erlang:put(observed, Next),
    {Alias, Next, Keep}.
