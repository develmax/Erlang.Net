-module(beam_constant_call_sequence).
-export([run/2, single/1]).
run(Key, Value) ->
    Prepared = prepare(Value),
    put(Key, Prepared),
    ok.
single(Key) ->
    _Previous = erase(Key),
    done.
prepare(Value) -> {prepared, Value}.
