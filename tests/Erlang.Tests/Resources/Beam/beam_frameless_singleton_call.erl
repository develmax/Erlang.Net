-module(beam_frameless_singleton_call).
-export([call/1]).
call(Value) ->
    done(Value),
    ready.
done(Value) ->
    erlang:put(saved, Value),
    ready.
