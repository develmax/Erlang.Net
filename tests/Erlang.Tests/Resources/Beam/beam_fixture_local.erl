-module(beam_fixture_local).
-export([run/0]).
run() -> loop(3,[]).
loop(0,Acc) -> {ok,Acc};
loop(N,Acc) -> loop(N-1,[N|Acc]).
