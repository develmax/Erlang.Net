-module(queue_callback368_regression).
-export([run/0,invoke/2,wrong/0]).

run() ->
    Old = erlang:system_flag(backtrace_depth,8),
    try
        [badarity(filter),
         badarity(filtermap),
         badarity(any),
         badarity(all),
         badarity(delete_with),
         badarity(delete_with_r),
         expansion()]
    after erlang:system_flag(backtrace_depth,Old) end.

badarity(Operation) ->
    try erlang:apply(queue,Operation,[fun bad/1,{[3,2],[1]}]) of
        Value -> {unexpected,Value}
    catch Class:Reason:Stack -> {Operation,Class,Reason,Stack} end.

bad(Value) -> invoke(fun wrong/0,[Value]).

invoke(Function,Arguments) -> erlang:apply(Function,Arguments).
wrong() -> wrong.

expansion() ->
    try queue:filter(fun improper/1,{[3,2,1],[]}) of
        Value -> {unexpected,Value}
    catch Class:Reason:Stack -> {expansion,Class,Reason,Stack} end.

improper(Value) -> [Value|callback_tail].
