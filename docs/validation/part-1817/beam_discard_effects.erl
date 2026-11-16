-module(beam_discard_effects).
-export([run/2, implicit/1]).

run(Tag, Values) ->
    _ = [note(Tag, Value) || Value <- Values],
    ok.

implicit(Values) ->
    [note(implicit, Value) || Value <- Values],
    ok.

note(Tag, Value) ->
    Old = case get(effects) of undefined -> []; List -> List end,
    put(effects, [{Tag,Value}|Old]),
    Value.
