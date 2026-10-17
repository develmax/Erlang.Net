-module(common_public_wrapper_clause).
-export([run/0,leaf/1]).

run() ->
    Old = erlang:system_flag(backtrace_depth,8),
    try
        [{wrapped,done} = {wrapped,common_public_wrapper_clause:leaf(Depth)} || Depth <- [0,1,2,8]],
        done
    after erlang:system_flag(backtrace_depth,Old)
    end.

leaf(Depth) ->
    erlang:system_flag(backtrace_depth,Depth),
    try proc_lib:start_link(42,init,[],10,[])
    catch error:function_clause:Stack ->
        [{proc_lib,start_link,[42,init,[],10,[]],_}|_] = Stack,
        true = length(Stack) =< erlang:max(1,Depth),
        done
    end.
