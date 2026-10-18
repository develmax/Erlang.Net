-module(lists_unique_source_contract).
-export([run/0]).
run() ->
    {lists:umerge([]),lists:umerge([[1,3],[2,3],[0,4]]),
     lists:umerge([[2.0],[1,2]]),lists:umerge(groups(64,[])),
     [capture(F) || F <- [sort_shape,merge_shape,merge_two_tail]]}.
groups(0,Acc) -> Acc;
groups(N,Acc) -> groups(N-1,[[N,N+1,N+2]|Acc]).
capture(Function) ->
    Old=erlang:system_flag(backtrace_depth,8),
    try choose(Function) of Value -> {unexpected,Value}
    catch Class:Reason:Stack -> {Class,Reason,Stack}
    after erlang:system_flag(backtrace_depth,Old) end.
choose(sort_shape) -> lists:usort([a|tail]);
choose(merge_shape) -> lists:umerge(raw);
choose(merge_two_tail) -> lists:umerge([[1|left],[2|right]]).
