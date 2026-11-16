-module(list_entry_fallback_probe).
-export([scan/2, fold/2]).

scan([Head|Tail], Acc) ->
    Value = lists:reverse(Head),
    case lists:member(Value, Tail) of
        true -> {Value,Acc};
        false -> scan(Tail, [Value|Acc])
    end;
scan([], Acc) ->
    {[],Acc}.

fold([Head|Tail], Check) ->
    Value = lists:reverse(Head),
    Check(Value),
    fold(Tail, Check);
fold([], Check) when is_function(Check, 1) ->
    [].
