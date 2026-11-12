-module(beam_guard_grouping_probe).
-export([guarded/2]).
-define(MASKED(A), (is_integer(A) andalso ((A) band 16#ff) =:= A)).
guarded(A, B) when ?MASKED(A) andalso (((B) bor (A)) band (bnot 16#ff)) =:= 0 -> ok;
guarded(_, _) -> error.
