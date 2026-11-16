-module(beam_singleton_call_result).
-export([call/1, neq/1]).
call(Value) ->
    done(Value),
    ready.
neq(Value) when Value =/= ready -> {other, Value};
neq(_) -> ready.
done(Value) ->
    erlang:put(saved, Value),
    ready.
