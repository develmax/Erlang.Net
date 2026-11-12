-module(beam_length_fun_guard_probe).
-export([one/1, two/2, len/1, zero/1, fixed/1]).
one(F) when is_function(F) -> yes;
one(_) -> no.
two(F,N) when is_function(F,N) -> yes;
two(_,_) -> no.
len(L) when length(L) =:= 2 -> yes;
len(_) -> no.
zero(L) when length(L) =:= 0 -> yes;
zero(_) -> no.

fixed(F) when is_function(F,2) -> yes;
fixed(_) -> no.
