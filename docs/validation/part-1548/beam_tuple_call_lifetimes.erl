-module(beam_tuple_call_lifetimes).
-export([run/1]).
run({First, Second, Third}) ->
    mark(prefix, First, Second),
    mark(middle, Second, Third),
    result(First, Third).
mark(prefix, stop, _) -> erlang:error(first);
mark(Stage, A, B) -> put(Stage, {A,B}).
result(A, B) -> {A,B,get(prefix),get(middle)}.
