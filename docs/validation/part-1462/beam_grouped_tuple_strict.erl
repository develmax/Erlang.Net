-module(beam_grouped_tuple_strict).
-export([strict/1]).
strict({same, X, _}) -> X;
strict({same, _, X}) -> X;
strict({same, X}) -> X;
strict({other, _, X}) -> X.
