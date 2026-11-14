-module(beam_probe).
-export([read/1, check/1, open/0, finish/1]).
read(V) -> put(read, V), V.
check(V) -> V =/= blocked.
open() -> case get(read) of closed -> closed; _ -> ok end.
finish(V) -> put(finished, V), finished.
