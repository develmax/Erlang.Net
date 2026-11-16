-module(beam_call_literal_prefix).
-export([run/2, consume/1]).

run(Key, Tail) ->
    erlang:put(prefix_key, Key),
    ?MODULE:consume("All " ++ Tail),
    {done, Key}.

consume(Value) ->
    erlang:put(prefix_value, {value, Value}).
