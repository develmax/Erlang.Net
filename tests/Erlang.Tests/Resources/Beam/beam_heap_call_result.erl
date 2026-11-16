-module(beam_heap_call_result).
-export([run/2]).
run(Value, Mode) ->
    X = seed(Value),
    case Mode of
        left -> A = step(X), B = step(A), {A, B};
        right -> C = step(X), D = step(C), {C, D};
        _ -> X
    end.
seed(X) -> erlang:garbage_collect(), X.
step(X) -> erlang:garbage_collect(), {step, X}.
