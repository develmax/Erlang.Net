-module(beam_guard_pid_size_probe).
-export([pid/1, size/1]).
pid(P) when is_pid(P) -> accepted;
pid(_) -> rejected.
size(T) when tuple_size(T) =:= 2 -> accepted;
size(_) -> rejected.
