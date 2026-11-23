-module(bitwise_range_probe).
-export([session/2, adjacent/1, compare/2, negative/2, dynamic/2, checked/2]).
session(Value,N) ->
    T = mask(Value),
    S = (T bsl 20) band 16#ffffffff,
    O = bitwise_range_probe_peer:opaque(N) - 1,
    {S bor O,T bsl 32}.
mask(Value) ->
    V = bitwise_range_probe_peer:opaque(Value),
    V band 16#ffffffff.
adjacent(Value) -> ((Value band 63) bsl 4) + 2.
compare(Key,Limit) ->
    A = erlang:phash2(Key,512) + 1,
    if
        A > Limit -> above;
        true -> other
    end.
negative(Value,Other) -> (Value band -1) bor Other.
dynamic(Value,Offset) -> ((Value band 63) bsl Offset) + 2.
checked(Value,Limit) when (Value band 63) > Limit -> above;
checked(_,_) -> other.
