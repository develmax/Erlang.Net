-module(beam_guarded_branch_stack_slots).
-export([run/2]).
run(Value, Mode) when is_atom(Mode) ->
    X = seed(Value),
    Y = case Mode of
        left -> A = step(X), {A, step(A)};
        right -> B = step(X), {step(B), B};
        _ -> X
    end,
    Y.
seed(X) -> erlang:garbage_collect(), X.
step(X) -> erlang:garbage_collect(), {step, X}.
