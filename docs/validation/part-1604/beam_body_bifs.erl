-module(beam_body_bifs).
-export([run/4, lookup/2]).
run(A,B,C,D) ->
    {node(),node(A),tuple_size(B),max(C,D),min(C,D)}.
lookup(Key, Map) ->
    {is_map_key(Key, Map), map_get(Key, Map)}.
