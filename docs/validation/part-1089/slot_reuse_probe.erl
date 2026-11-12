-module(slot_reuse_probe).
-export([reuse/4, keep/4, duplicate/3, closure/2]).
reuse(Value, Key, Once, Keep) ->
    Old = erlang:put(Key, Value),
    erlang:put(Once, Keep),
    {Old, Keep}.
keep(Value, Key, Twice, Keep) ->
    Old = erlang:put(Key, Value),
    erlang:put(Twice, Keep),
    {Old, Twice}.
duplicate(Value, Key, Twice) ->
    Old = erlang:put(Key, Value),
    erlang:put(Twice, Twice),
    {Old, Value}.
closure(Key, Once) ->
    fun(Value, Keep) ->
        Old = erlang:put(Key, Value),
        erlang:put(Once, Keep),
        {Old, Keep}
    end.
