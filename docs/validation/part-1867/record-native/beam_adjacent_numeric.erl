-module(beam_adjacent_numeric).
%% options
-export([value/2]).
-record(row, {name, value, a, b, c, d}).
value(R = #row{}, Input) ->
    Replacement = Input + 1,
    R#row{value = Replacement}.
