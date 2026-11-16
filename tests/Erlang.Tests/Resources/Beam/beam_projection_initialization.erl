-module(beam_projection_initialization).
-export([run/1]).
run(List) ->
    lists:map(fun({Left, Right}) ->
        erlang:garbage_collect(),
        {Right, Left}
    end, List).
