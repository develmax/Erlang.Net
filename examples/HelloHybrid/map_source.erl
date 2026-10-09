-module(map_source).
-export([run/0, pick/1, bits/0, bit_guard/1]).
run() ->
    M = #{1 => int, 1.0 => float, value => 40},
    N = M#{value := 42, extra => ok},
    #{1 := int, 1.0 := float, value := X} = N,
    X.
pick(#{value := {X, X}}) when is_map(#{a => ok}), map_size(#{a => ok}) =:= 1,
    is_map_key(value, #{value => {X, X}}), map_get(value, #{value => {X, X}}) =:= {X, X} -> X;
pick(_) -> no.

bits() -> <<4660:16/little, 5:3, 17:5, (<<1:1>>)/bitstring>>.

bit_guard(Bits) when is_bitstring(Bits), bit_size(Bits) =:= 25, byte_size(Bits) =:= 4 -> ok;
bit_guard(_) -> no.
