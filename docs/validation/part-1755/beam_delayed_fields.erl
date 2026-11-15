-module(beam_delayed_fields).
-export([keep/1, early/1, assign/2, reuse/2]).

keep(T = {tag, Value}) ->
    erlang:garbage_collect(),
    {T, Value}.

early({Left, Right}) ->
    erlang:put(tag, Left),
    {Left, Right}.

assign({Left, Right}, Key) ->
    Previous = erlang:put(saved, Key),
    {Previous, Left, Right}.

reuse(T = {tag, Value}, Key) ->
    Old = erlang:put(Key, T),
    {Old, T, Value}.
