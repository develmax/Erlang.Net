-module(computed_argument_probe).
-export([first/1, second/1, multiple/0]).

first(Value) -> {erlang:put(erlang:get(lookup), Value), done}.
second(Key) -> {erlang:put(Key, erlang:get(lookup)), done}.
multiple() -> {erlang:put(erlang:put(ordered, updated), erlang:get(ordered)), done}.
