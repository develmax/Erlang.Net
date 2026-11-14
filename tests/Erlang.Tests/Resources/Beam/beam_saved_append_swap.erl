-module(beam_saved_append_swap).
-export([run/2, produce/1, consume/1]).

run(Value, Prefix) ->
    Saved = ?MODULE:produce(Value),
    ?MODULE:consume(Prefix ++ " only"),
    {done, Saved}.

produce(Value) -> {held, Value}.

consume(Value) -> erlang:put(prefix, {value, Value}).
