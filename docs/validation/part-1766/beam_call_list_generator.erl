-module(beam_call_list_generator).
-export([project/2, map/2, identity/1, embedded/2]).

project(List, Prefix) ->
    [{Key, Prefix, Value} || {Key, Value} <- source(List)].

map(List, Key) ->
    [item(Key, E) || E <- tl(List)].

identity(List) ->
    [E || E <- source(List)].

embedded(List, Prefix) ->
    {tag, [{Key, Prefix, Value} || {Key, Value} <- lists:reverse(List)]}.

source(List) ->
    put(source_count, 1 + get(source_count)),
    List.

item(Key, Value) ->
    {Key, Value}.