-module(beam_tuple_atom_tail).
-export([run/2]).
run(z, X) -> emit(X, 1);
run(a, X) -> emit(X, 2);
run(m, _) -> emit(none, 3);
run({tag, _}, X) -> emit(X, 4);
run(Other, X) -> emit(X, Other).
emit(X, Y) -> {X, Y}.
