-module(computed_tail_probe).
-export([convert/1, transform/3, call_fixed/2]).

convert(Tuple) ->
    Reserved = reserved(),
    finish(tuple_to_list(Tuple), Reserved, []).

reserved() ->
    erlang:get(tail_token).

finish(Items, Reserved, Tail) ->
    {Items,Reserved,Tail}.

transform(Input, Prefix, State) ->
    Values = normalize(Input),
    lists:mapfoldl(fun(Entry, Acc) -> {{Prefix,Entry},Acc+1} end, State, Values).

call_fixed(Input, State) ->
    fixed(Input, fixed, State).

fixed(Input, Prefix, State) ->
    Values = normalize(Input),
    lists:mapfoldl(fun(Entry, Acc) -> {{Prefix,Entry},Acc+1} end, State, Values).

normalize(Input) ->
    lists:reverse(Input).
