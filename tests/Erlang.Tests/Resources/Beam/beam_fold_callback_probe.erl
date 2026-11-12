-module(beam_fold_callback_probe).
-export([run/2, single/2, record_fold/1]).
-record(fold_record, {key, extra}).

run(L, Offset) ->
    lists:foldl(fun
        (N, Acc) when is_integer(N) -> [N+Offset|Acc];
        (skip, Acc) -> Acc
    end, [], L).

single(L, Offset) ->
    lists:foldl(fun (N, Acc) -> [N+Offset|Acc] end, [], L).

record_fold(Record) ->
    Keys = [first,skip,second] -- [skip],
    Values = lists:foldl(fun (Key, Acc) ->
        [{Key, Record#fold_record.key}|Acc]
    end, [], Keys),
    lists:reverse(Values) ++ Record#fold_record.extra.
