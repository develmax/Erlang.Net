-module(beam_saved_nil_argument).
-export([run/2, produce/1, consume/2]).

run(Key, Value) ->
    Saved = ?MODULE:produce(Value),
    ?MODULE:consume(Key, []),
    {done, Saved, Key}.

produce(Value) -> {held, Value}.

consume(Key, Options) ->
    erlang:put(Key, {options, Options}).
