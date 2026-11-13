-module(beam_binary_multiple_fields).
-export([split/3]).
split(Bin, First, Skip) when is_binary(Bin) ->
    <<Prefix:First/binary, _:Skip/binary, Middle:2/binary, Rest/binary>> = Bin,
    {Prefix,Middle,Rest}.
