-module(beam_stack_call_order).
-export([run/3, run4/4]).

run(Tag, First, Second) ->
    combine(lists:concat([Tag, ".tag"]), First, Second).

run4(Tag, First, Second, Third) ->
    Value = build(Tag),
    combine4(Value, First, Second, Third).

build(Tag) -> put(last_tag, Tag), Tag.
combine(Text, A, B) -> {Text,A,B}.
combine4(Tag, A, B, C) -> {Tag,A,B,C}.
