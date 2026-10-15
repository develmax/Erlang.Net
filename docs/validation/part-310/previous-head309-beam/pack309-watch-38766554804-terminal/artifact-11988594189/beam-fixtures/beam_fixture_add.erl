-module(beam_fixture_add).
-export([run/0,add/2]).
run() -> add(17,25).
add(A,B) when is_integer(A), is_integer(B) -> A+B.
