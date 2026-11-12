-module(tuple_register_probe).
-export([first/1, middle/2, last/1, multiple/0]).

first(Key) -> {erlang:get(Key), first}.
middle(Key, Label) -> {Label, erlang:get(Key), middle}.
last(Value) -> {last, Value, Value + 1}.
multiple() -> {erlang:put(ordered, updated), erlang:get(ordered), multiple}.
