-module(size_source_location_contract).
-export([run/0,tuple_context/1,bits_context/1,bytes_context/1,map_context/1]).
run() ->
    {[observe(Function,Depth) || Function <- [tuple_context,bits_context,bytes_context,map_context], Depth <- [default,0,1,2,8]],
     [guard_tuple(invalid),guard_bits(invalid),guard_bytes(invalid),guard_map(invalid)]}.
observe(Function,Depth) ->
    Selected = case Depth of default -> 8; _ -> Depth end,
    Old = erlang:system_flag(backtrace_depth,Selected),
    try erlang:apply(size_source_location_contract,Function,[invalid]) of
        Value -> {unexpected,Value}
    catch Class:Reason:Stack -> {Function,Depth,Class,Reason,Stack}
    after erlang:system_flag(backtrace_depth,Old) end.
tuple_context(Value) -> Size=erlang:tuple_size(Value), {returned,Size}.
bits_context(Value) -> Size=erlang:bit_size(Value), {returned,Size}.
bytes_context(Value) -> Size=erlang:byte_size(Value), {returned,Size}.
map_context(Value) -> Size=erlang:map_size(Value), {returned,Size}.
guard_tuple(Value) when erlang:tuple_size(Value) >= 0 -> true;
guard_tuple(_) -> false.
guard_bits(Value) when erlang:bit_size(Value) >= 0 -> true;
guard_bits(_) -> false.
guard_bytes(Value) when erlang:byte_size(Value) >= 0 -> true;
guard_bytes(_) -> false.
guard_map(Value) when erlang:map_size(Value) >= 0 -> true;
guard_map(_) -> false.
