-module(beam_saved_literal_arguments).
-export([run/2, produce/1, consume/3]).

run(Key, Value) ->
    Saved = ?MODULE:produce(Value),
    ?MODULE:consume(Key, 1, [option]),
    {done, Saved, Key}.

produce(Value) -> {held, Value}.

consume(Key, Id, Options) ->
    erlang:put(Key, {Id, Options}).
