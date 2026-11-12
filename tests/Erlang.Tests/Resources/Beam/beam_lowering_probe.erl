-module(beam_lowering_probe).
-export([header/0, swap_args/2, pattern/1, ordinary/1]).
-include("beam_lowering_header.hrl").
swap_args(A,B) -> target(B,A).
target(_,Y) -> Y.
pattern(ok) -> yes;
pattern(_) -> no.
ordinary(X) ->
    target(X,X),
    ok.
