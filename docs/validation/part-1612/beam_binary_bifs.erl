-module(beam_binary_bifs).
-export([run/3]).
run(A,B,M) ->
    {max(mark(left,A),mark(right,B)),min(mark(left,A),B),
     map_get(mark(key,A),mark(map,M)),is_map_key(A,mark(map,M))}.
mark(Tag,Value) -> put(last,Tag), Value.
