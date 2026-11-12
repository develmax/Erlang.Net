-module(beam_call_sequence_tuple_probe).
-export([plain/2, retained/2, closure/1, captured/1]).

plain(Key, Pairs) ->
    Value = proplists:get_value(Key, Pairs),
    Sorted = lists:sort(Value),
    {sorted,Sorted}.

retained(Key, Pairs) ->
    Value = proplists:get_value(Key, Pairs),
    Sorted = lists:sort(Value),
    {Key,Value,Sorted}.

closure(Input) ->
    Sorted = lists:sort(pass(Input)),
    F = fun() -> ok end,
    {sorted,Sorted,F}.

captured(Input) ->
    Sorted = lists:sort(Input),
    {Sorted,fun() -> Sorted end}.

pass(X) -> X.



