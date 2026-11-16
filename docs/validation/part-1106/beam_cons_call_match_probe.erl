-module(beam_cons_call_match_probe).
-export([run/1, loop/1]).
run([H|T]) ->
    true = valid(H),
    loop(T).
loop([]) -> ok;
loop([H|T]) ->
    true = valid(H),
    loop(T).
valid(Value) -> Value.
