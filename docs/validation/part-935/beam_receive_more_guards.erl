-module(beam_receive_more_guards).
-export([run/1]).
run(M) ->
    receive
        {k,K} when is_map_key(K,M) -> {key,K};
        {n,V} when not is_tuple(V), V =:= [1]; is_list(V), V == [2] -> {list,V}
    after 0 -> timeout
    end.
