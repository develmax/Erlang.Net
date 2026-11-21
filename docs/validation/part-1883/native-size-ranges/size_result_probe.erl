-module(size_result_probe).
-export([bytes/1, bits/1, items/1, above/2, same/2, unknown/1, guard_path/2, other/2]).
bytes(Bin) -> byte_size(Bin) + 1.
bits(Bits) -> bit_size(Bits) rem 8.
items(Map) -> map_size(Map) - 1.
above(Bin, Limit) -> byte_size(Bin) >= Limit.
same(Bin, Limit) -> byte_size(Bin) =:= Limit.
unknown(Value) -> size_result_probe_peer:byte_size(Value) + 1.
guard_path(Bin, Minimum) when byte_size(Bin) > Minimum -> big;
guard_path(_, _) -> none.
other(Bin, Value) ->
    Size = byte_size(Bin),
    case Value of
        {size, K} -> K + 1;
        _ -> {Size, Value}
    end.
