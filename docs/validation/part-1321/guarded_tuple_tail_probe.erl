-module(guarded_tuple_tail_probe).
-export([route/3, preserve/3, target/1]).
route(A, {tag,N}, Level) when N >= Level -> target({wrapped,A});
route(_,_,_) -> no.
preserve(A, {tag,N}, Level) when N >= Level -> target({wrapped,A,N});
preserve(_,Other,_) -> Other.
target(A) -> A.
