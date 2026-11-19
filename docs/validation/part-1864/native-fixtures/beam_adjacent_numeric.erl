-module(beam_adjacent_numeric).
%% options
-export([value/2, choose/2]).
value(Tag, Value) ->
    case choose(Tag, Value) of
        {_, Result} -> Result;
        _ -> absent
    end.
choose(Tag, Value) ->
    case Tag of
        found -> {ok, Value};
        extra -> {other, Value, 0};
        refused -> erlang:error({refused, Value});
        _ -> error
    end.
