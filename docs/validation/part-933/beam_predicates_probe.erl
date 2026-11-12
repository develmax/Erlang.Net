-module(beam_predicates_probe).
-export([range/3,constant/1,record/1,value/3]).
range(N,L,H) when is_integer(N,L,H) -> yes;
range(_,_,_) -> no.
constant(N) when is_integer(N,2,9) -> yes;
constant(_) -> no.
record(X) when is_record(X) -> yes;
record(_) -> no.
value(N,L,H) -> {is_integer(N,L,H),is_record(N)}.
