-module(frame_bif_probe).
-compile({no_auto_import, [get/1, self/0]}).
-export([explicit/2, shadowed/2]).

explicit(Key, Tag) ->
    {Tag, erlang:get(Key), erlang:get(fixed), erlang:self()}.

shadowed(Key, Tag) ->
    {Tag, get(Key), self()}.

get(Key) -> {local, Key}.
self() -> local_self.
