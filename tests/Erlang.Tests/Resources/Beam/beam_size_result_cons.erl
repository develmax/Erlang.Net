-module(beam_size_result_cons).
-export([known_cons/2]).
known_cons(Head, Tail) -> length([Head|Tail]) + 1.
