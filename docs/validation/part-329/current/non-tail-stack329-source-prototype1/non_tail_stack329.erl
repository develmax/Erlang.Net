-module(non_tail_stack329).
-export([tail/1, non_tail/1, anonymous/1, unwind/1, error_unwind/1]).

tail(N) -> tail(N, 0).
tail(0, Acc) -> Acc;
tail(N, Acc) when N > 0 -> tail(N - 1, Acc + 1).

non_tail(0) -> 0;
non_tail(N) when N > 0 -> 1 + non_tail(N - 1).

anonymous(N) ->
    Walk = fun(_, 0) -> 0;
              (Again, Left) when Left > 0 -> 1 + Again(Again, Left - 1)
           end,
    Walk(Walk, N).

unwind(N) ->
    put(stack329_after, 0),
    Value = unwind_value(N),
    {Value, erase(stack329_after)}.
unwind_value(0) -> 0;
unwind_value(N) when N > 0 ->
    try unwind_value(N - 1) of Value -> Value + 1
    after put(stack329_after, get(stack329_after) + 1)
    end.

error_unwind(N) ->
    put(stack329_after, 0),
    try error_value(N)
    catch error:stack329_leaf -> {caught, erase(stack329_after)}
    end.
error_value(0) -> erlang:error(stack329_leaf);
error_value(N) when N > 0 ->
    try error_value(N - 1)
    after put(stack329_after, get(stack329_after) + 1)
    end.
