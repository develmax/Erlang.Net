-module(guarded_atom_tail_probe).
-export([run/2]).

run([Head|_], Input) when is_atom(Head) ->
    case mode(Input) of
        first ->
            Value = maps:get(value, Input),
            done(Head, Value);
        second ->
            done(Head, fallback)
    end;
run(_, _) -> skipped.

mode(Input) -> maps:get(mode, Input).
done(Head, Value) -> {Head, Value}.
