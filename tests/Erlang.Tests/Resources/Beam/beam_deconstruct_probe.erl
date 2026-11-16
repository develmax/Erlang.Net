-module(beam_deconstruct_probe).
-export([tagged/1,head/1,tail/1]).
tagged({ok, X}) -> X;
tagged(_) -> error.
head([H|_]) -> H;
head(_) -> error.
tail([_|T]) -> T;
tail(_) -> error.
