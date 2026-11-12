-module(literal_match_probe).
-export([run/0, discarded/0, mismatch/0]).
run() -> pair(sentinel = sentinel, 7 = 7).
discarded() -> unused = unused, kept.
mismatch() -> 7 = 8.
pair(A,B) -> {A,B}.
