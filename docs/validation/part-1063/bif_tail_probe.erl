-module(bif_tail_probe).
-export([first/1, second/1, swap/2, multiple/2, pack/3]).

first(Key) -> ?MODULE:pack(erlang:get(Key), Key, first).
second(Key) -> ?MODULE:pack(Key, erlang:get(Key), second).
swap(Key, Tag) -> ?MODULE:pack(Tag, Key, erlang:self()).
multiple(Key, Label) -> pack(erlang:get(Key), Label, erlang:node()).
pack(A, B, C) -> {A, B, C}.
