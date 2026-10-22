-module(bounded_bif_contract).
-export([run/0,hd_context/1,tl_context/1,tuple_context/1,integer_context/1]).
run() ->
    {[observe(Function,Depth) || Function <- [hd_context,tl_context,tuple_context,integer_context], Depth <- [default,0,1,2,8]],
     guards(),identity(),ranges()}.
observe(Function,Depth) ->
    Selected = case Depth of default -> 8; _ -> Depth end,
    Old = erlang:system_flag(backtrace_depth,Selected),
    try erlang:apply(bounded_bif_contract,Function,[invalid]) of
        Value -> {unexpected,Value}
    catch Class:Reason:Stack -> {Function,Depth,Class,Reason,Stack}
    after erlang:system_flag(backtrace_depth,Old) end.
hd_context(Value) -> Result=erlang:hd(Value), {returned,Result}.
tl_context(Value) -> Result=erlang:tl(Value), {returned,Result}.
tuple_context(Value) -> Result=erlang:tuple_size(Value), {returned,Result}.
integer_context(Value) -> Result=erlang:is_integer(Value,invalid_bound,10), {returned,Result}.
guards() ->
    [guard_hd([sentinel]),guard_hd(invalid),guard_tl([head|sentinel]),guard_tl(invalid),
     guard_tuple({}),guard_tuple(invalid),guard_integer(1,0,2),guard_integer(1,invalid_bound,2)].
guard_hd(Value) when erlang:hd(Value) =:= sentinel -> true;
guard_hd(_) -> false.
guard_tl(Value) when erlang:tl(Value) =:= sentinel -> true;
guard_tl(_) -> false.
guard_tuple(Value) when erlang:tuple_size(Value) >= 0 -> true;
guard_tuple(_) -> false.
guard_integer(Value,Lower,Upper) when erlang:is_integer(Value,Lower,Upper) -> true;
guard_integer(_,_,_) -> false.
identity() ->
    Payload={payload,[head|tail],#{key=>value}},
    Source=[Payload|Payload],
    {erlang:hd(Source),erlang:tl(Source),Payload}.
ranges() ->
    Boundary=1 bsl 4096,
    {erlang:is_integer(Boundary,Boundary,Boundary),
     erlang:is_integer(Boundary,Boundary+1,Boundary+2),
     erlang:is_integer(-Boundary,-Boundary,-Boundary),
     erlang:is_integer(atom,-Boundary,Boundary),
     erlang:is_integer(0,Boundary,-Boundary)}.
