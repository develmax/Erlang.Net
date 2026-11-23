-module(beam_constructed_list_generator_helper).
-export([constructed/2, open_tail/1, reset/0, events/0, nested/1]).
constructed(Left, Right) ->
    [mapped(Value) || Value <- [produce(Left), produce(Right)]].
open_tail(Tail) ->
    [mapped(Value) || Value <-
          [produce(head)|Tail]].
nested(Values) ->
    [[mapped(Value) || Value <- [Item]] || Item <- Values].
reset() -> put(events, []), ok.
events() -> get(events).
produce(fail) -> event({produce,fail}), erlang:error(rejected_input);
produce(Value) -> event({produce,Value}), Value.
mapped(Value) -> event({mapped,Value}), Value.
event(Value) -> put(events, [Value|get(events)]).
