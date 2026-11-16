-module(beam_bif_predicate_frame).
-export([run/0]).
run() ->
    case get(beam_bif_predicate_frame) of
        undefined ->
            put(beam_bif_predicate_frame, populated),
            erlang:garbage_collect(),
            created;
        populated ->
            existing
    end.
