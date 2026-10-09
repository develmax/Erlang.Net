-module(map_source).
-export([run/0, pick/1]).
run() ->
    M = #{1 => int, 1.0 => float, value => 40},
    N = M#{value := 42, extra => ok},
    #{1 := int, 1.0 := float, value := X} = N,
    X.
pick(#{value := {X, X}}) -> X;
pick(_) -> no.
