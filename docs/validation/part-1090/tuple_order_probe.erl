-module(tuple_order_probe).
-export([tuple/4, select/4, strict/4, closure/2]).
tuple(Value, Key, Once, Keep) ->
    {erlang:put(Key, Value), erlang:put(Once, Keep), Keep}.
select(Value, Key, Once, Keep) ->
    case {erlang:put(Key, Value), erlang:put(Once, Keep)} of
        {old, Value} -> {ordered, Keep};
        {A, B} -> {other, A, B}
    end.
strict(Value, Key, Once, Keep) ->
    case {erlang:put(Key, Value), erlang:put(Once, Keep)} of
        {old, Value} -> {ordered, Keep}
    end.
closure(Key, Once) ->
    fun(Value, Keep) ->
        {erlang:put(Key, Value), erlang:put(Once, Keep), Keep}
    end.
