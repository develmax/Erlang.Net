-module(beam_relaxed_tuple_alias_helpers).
-export([guarded/2, project/1, recorded/1, strict/1]).
-record(entry, {id, state}).
guarded(Floor, Rows) ->
    [Whole || Whole = {tag, V} <- Rows, is_integer(V) andalso V > Floor].
project(Rows) ->
    [{Id, Whole} || {_, Id, _} = Whole <- Rows].
recorded(Rows) ->
    [{Id, Whole} || #entry{id=Id} = Whole <- Rows].
strict(Rows) ->
    [Whole || Whole = {tag, _} <:- Rows].
