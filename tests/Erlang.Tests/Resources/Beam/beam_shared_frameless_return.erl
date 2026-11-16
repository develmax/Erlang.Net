-module(beam_shared_frameless_return).
-export([run/1, consume/1]).
run({first, _, _}) -> ok;
run({second, _, _}) -> ok;
run(Value) ->
    erlang:put(last_value, Value),
    consume({need, Value}).
consume(Value) -> Value.
