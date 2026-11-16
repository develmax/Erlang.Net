-module(beam_grouped_tuple_projection).
-export([pick/1]).
pick({gamma, _, X, _}) -> X;
pick({beta, X, _, _}) -> X;
pick({alpha, _, X, _}) -> X;
pick({theta, _, X}) -> X;
pick({delta, X, _}) -> X;
pick({omega, X}) -> X;
pick(_) -> missing.
