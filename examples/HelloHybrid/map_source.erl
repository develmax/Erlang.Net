-module(map_source).
-export([run/0, pick/1]).
run() ->
    M = #{1 => int, 1.0 => float, value => 40},
    N = M#{value := 42, extra => ok},
    #{1 := int, 1.0 := float, value := X} = N,
    X.
pick(#{value := {X, X}}) when is_map(#{a => ok}), map_size(#{a => ok}) =:= 1,
    is_map_key(value, #{value => {X, X}}), map_get(value, #{value => {X, X}}) =:= {X, X} -> X;
pick(_) -> no.
