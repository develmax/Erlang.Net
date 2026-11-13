-module(beam_binary_register_bifs).
-export([run/4, extrema/2]).
run(A,B,C,D) ->
    {element(1,A),map_get(B,C),is_map_key(B,D)}.
extrema(A,B) ->
    {min(A,B),max(A,B),A}.
