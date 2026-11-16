-module(beam_shared_cons_payload).
-export([inspect/1]).
inspect([{row, first, {A,B}, Extra}|Tail]) -> {A,B,Extra,inspect(Tail)};
inspect([{row, second, Value, Extra}|Tail]) -> {Value,Extra,inspect(Tail)};
inspect([{other, Value}|_]) -> Value;
inspect([_|Tail]) -> inspect(Tail);
inspect([]) -> absent.
