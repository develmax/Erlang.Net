-module(beam_guarded_binary_split).
-export([split/2, swapped/2]).
split(Bin, Count) when is_binary(Bin), is_integer(Count) ->
    <<Part:Count/binary, Tail/binary>> = Bin,
    {Part,Tail}.
swapped(Count, Bin) when is_integer(Count), is_binary(Bin) ->
    <<Part:Count/binary, Tail/binary>> = Bin,
    {Tail,Part}.
