-module(bif_tail_probe).
-export([size/3, pad/4, split/1, bits/1, other/1]).

size(Tag, Data, Suffix) ->
    Bin = iolist_to_binary(Data),
    keep(Tag, Bin, erlang:byte_size(Bin), Suffix).

keep(Tag, Bin, Size, Suffix) ->
    {Tag,Bin,Size,Suffix}.

pad(Code, Flags, Vendor, Data) ->
    Size = iolist_size(Data),
    packed(Code, Flags, Size, Vendor, Data, (4 - Size rem 4) rem 4).

packed(Code, Flags, Size, Vendor, Data, Pad) ->
    {Code,Flags,Size,Vendor,Data,Pad}.

split(Data) ->
    Size = iolist_size(Data),
    result(Size, (Size rem 4) + (Size rem 3)).

bits(Data) ->
    Bin = iolist_to_binary(Data),
    result(Bin, bit_size(Bin)).

result(Value, Count) ->
    {Value,Count}.

other(Data) ->
    Bin = iolist_to_binary(Data),
    result(Bin, lists:byte_size(Bin)).
