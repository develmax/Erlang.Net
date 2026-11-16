-module(beam_closure_argument_tail).
-export([run/2, consume/1]).

run(_Unused, Value) ->
    ?MODULE:consume(fun() -> captured(Value) end).

consume(Fun) ->
    Fun().

captured(Value) -> {captured, Value}.
