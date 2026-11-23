-module(fixture_record_dep).
-export([select/2,send/3]).
select(A, _Key) -> A.
send(A, B, C) -> {A,B,C}.
