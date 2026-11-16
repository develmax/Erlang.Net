-module(beam_disjoint_range).
-export([run/1]).

run(1) -> true;
run(2) -> true;
run(X) when X >= 10, X =< 20 -> true;
run(X) when X >= 30, X =< 40 -> true;
run(X) when X >= 50, X =< 60 -> true;
run(_) -> false.
