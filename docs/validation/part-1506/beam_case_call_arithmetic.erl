-module(beam_case_call_arithmetic).
-export([left/2, right/2, followed/2, opaque/1]).

left(Input, Offset) ->
    case ?MODULE:opaque(Input) - Offset of
        0 -> equal;
        Difference -> Difference
    end.

right(Input, Offset) ->
    case Offset - ?MODULE:opaque(Input) of
        Difference when Difference < 0 -> {smaller, Offset};
        Difference -> Difference
    end.

followed(Input, Offset) ->
    Difference = case ?MODULE:opaque(Input) - 1 of
        0 -> equal;
        Nonzero -> Nonzero
    end,
    {Difference, Offset}.

opaque(Input) ->
    erlang:abs(Input).
