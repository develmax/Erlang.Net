-module(tuple_entry_tail_probe).
-export([route/2, preserve/2, guarded/2, target/2]).
route({tag,A}, B) -> target({wrapped,A}, B);
route(_,_) -> no.
preserve({tag,A}, B) -> target({wrapped,A,B}, A);
preserve(_,_) -> no.
guarded({tag,A}, B) when is_atom(A) -> target({wrapped,A}, B);
guarded(_,_) -> no.
target(A,B) -> {A,B}.
