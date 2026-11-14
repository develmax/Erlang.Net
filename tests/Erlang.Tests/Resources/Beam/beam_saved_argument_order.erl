-module(beam_saved_argument_order).
-export([run/3, produce/1, consume/3]).

run(First, Second, Value) ->
    Saved = ?MODULE:produce(Value),
    ?MODULE:consume(First, Second, []),
    {done, Saved, First, Second}.

produce(Value) -> {held, Value}.

consume(First, Second, Options) ->
    erlang:put(First, {Second, Options}).
