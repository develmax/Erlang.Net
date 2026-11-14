-module(beam_short_append).
-export([short/1, limit/1, long/1]).
short(Tail) -> " " ++ Tail.
limit(Tail) -> "abcd" ++ Tail.
long(Tail) -> "abcde" ++ Tail.
