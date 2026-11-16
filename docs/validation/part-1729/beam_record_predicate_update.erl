-module(beam_record_predicate_update).
-export([guard/3, predicate/2, update/3, effects/0]).
-record(item, {value}).
-record(other, {value, kind}).
guard(A, Value, B) when is_record(Value, item); is_record(Value, other) ->
    {left(A, Value), right(B, Value)};
guard(A, _, B) -> {ignored, A, B}.
predicate(A, Value) -> {A, is_record(Value, item)}.
update(A, Value, Replacement) -> {A, Value#item{value = touch(Replacement)}}.
left(A, Value) -> {left, A, Value}.
right(B, Value) -> {right, B, Value}.
touch(Value) -> erlang:put(record_effect, Value), Value.
effects() -> erlang:erase(record_effect).
