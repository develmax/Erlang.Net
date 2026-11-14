-module(beam_trim_tuple_slots).
-export([run/6, consume/3]).

run(Keep, A, B, Other, C, Retain) ->
    ?MODULE:consume(A, B, C),
    ?MODULE:consume(C, A, B),
    {Keep, Other, Retain}.

consume(A, B, C) ->
    erlang:put(A, {B, C}).
