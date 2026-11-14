-module(beam_reuse_saved_literal).
-export([run/3, produce/1, consume/3, finish/2]).

run(Key, Drop, Value) ->
    Saved = ?MODULE:produce(Value),
    ?MODULE:consume(Key, Drop, [option]),
    ?MODULE:finish(Key, Saved).

produce(Value) -> {held, Value}.

consume(Key, Drop, Options) ->
    erlang:put(Key, {Drop, Options}).

finish(Key, Saved) -> {done, Saved, Key}.
