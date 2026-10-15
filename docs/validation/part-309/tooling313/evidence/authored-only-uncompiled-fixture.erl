-module(beam_fixture_load_nif_opcodes).
-export([non_tail/3,tail_last/3,tail_only/2]).

non_tail(Path,Info,Marker) ->
    Result=erlang:load_nif(Path,Info),
    {Marker,Result}.

tail_last(Path,Info,Marker) ->
    erlang:put(nif_frame_marker,Marker),
    erlang:load_nif(Path,Info).

tail_only(Path,Info) ->
    erlang:load_nif(Path,Info).
