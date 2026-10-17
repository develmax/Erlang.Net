-module(generic_depth_error).
-export([run/0,leaf/1,middle/1]).
run()->
    Old=erlang:system_flag(backtrace_depth,8),
    try
        lists:foreach(fun(Depth)->erlang:system_flag(backtrace_depth,Depth),check(Depth) end,[0,1,2,8]),done
    after erlang:system_flag(backtrace_depth,Old) end.
check(Depth)->
    try middle(probe_reason) of _->error(missing_failure)
    catch error:Reason:Stack->
        probe_reason=Reason,
        [{generic_depth_error,leaf,1,Location}|Tail]=Stack,
        true=is_list(Location),
        case Depth of 0->[]=Tail;1->[]=Tail;_-> [{generic_depth_error,middle,1,_}|_]=Tail end,
        true=(length(Stack)=<erlang:max(1,Depth))
    end.
middle(V)->R=generic_depth_error:leaf(V),{middle_returned,R}.
leaf(V)->R=erlang:error(V),{leaf_returned,R}.
