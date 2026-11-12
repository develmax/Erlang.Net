-module(bif_tuple_probe).
-export([first/1, middle/2, keep/2, repeated/1, empty/0]).

first(Key) -> {erlang:get(Key), first}.
middle(Key, Label) -> {Label, erlang:get(Key), middle}.
keep(Key, _Ignored) -> {Key, erlang:self(), erlang:get(Key)}.
repeated(Key) -> {erlang:get(Key), erlang:self(), erlang:get(Key)}.
empty() -> {erlang:self(), erlang:node(), erlang:get(missing)}.
