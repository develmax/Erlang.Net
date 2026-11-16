-module(beam_call_prefix_spill).
-export([run/4, make/1, consume/4]).

run(Parent, Tail, Extra, Keep) ->
    Saved = ?MODULE:make(Parent),
    ?MODULE:consume(Parent, 1, "All " ++ Tail, []),
    ?MODULE:consume(Parent, 2, Tail, Extra),
    {done, Saved, Keep}.

make(Value) -> {held, Value}.

consume(Parent, Id, Value, Options) ->
    erlang:put(Parent, {Id, Value, Options}).
