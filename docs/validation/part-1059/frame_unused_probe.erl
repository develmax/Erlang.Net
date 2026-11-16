-module(frame_unused_probe).
-export([read/3, match/2, guarded/2, capture/2]).

read(Ignored, Key, _) ->
    {erlang:get(Key), erlang:self()}.

match(Ignored, Key) ->
    Key = erlang:get(fixed),
    {Key, erlang:self()}.

guarded(Original, Key) when is_atom(Key) ->
    {erlang:get(Key), erlang:self()}.

capture(Ignored, Key) ->
    {fun() -> Key end, erlang:self()}.

