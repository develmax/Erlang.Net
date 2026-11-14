-module(beam_shared_cons_tag).
-export([inspect/1]).
inspect([{row, first, {A,B}}|Tail]) -> {A, B, inspect(Tail)};
inspect([{row, second, Value}|Tail]) -> {Value, inspect(Tail)};
inspect([{other, C}|_]) -> C;
inspect([_|Tail]) -> inspect(Tail);
inspect([]) -> absent.
