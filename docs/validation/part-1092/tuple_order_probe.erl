-module(tuple_order_probe).
-export([tuple/4, select/4, strict/4, closure/2, repeated/2, exported/2, literal/4, referenced/4]).
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

repeated(First, Second) ->
    case {First, Second} of
        {Same, Same} when is_atom(Same) -> {yes, Same};
        _ -> no
    end.
exported(First, Second) ->
    case {First, Second} of
        {tag, Selected} -> ok;
        {Selected, _} -> ok
    end,
    {value, Selected}.

literal(Value, Key, Once, Keep) ->
    case {erlang:put(Key, Value), erlang:put(Once, Keep)} of
        {old, expected} -> {ordered, Keep}
    end.
referenced(Value, Key, Once, Keep) ->
    First = erlang:put(Key, Value),
    Second = erlang:put(Once, Keep),
    case {First, Second} of
        {old, expected} -> {Second, Keep}
    end.
