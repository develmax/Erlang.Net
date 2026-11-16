-module(frame_remote_probe).
-export([lookup/2]).
lookup(Key, Tag) -> {Tag, persistent_term:get(Key), get(Key), self()}.
