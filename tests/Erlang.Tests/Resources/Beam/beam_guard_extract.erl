-module(beam_guard_extract).
-export([tail/2,part/4]).
tail(A,B) when tl(A) =:= B -> yes;
tail(_,_) -> no.
part(A,S,L,B) when binary_part(A,S,L) =:= B -> yes;
part(_,_,_,_) -> no.
