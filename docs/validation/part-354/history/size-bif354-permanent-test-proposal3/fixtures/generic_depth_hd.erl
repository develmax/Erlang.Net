-module(generic_depth_hd).
-export([run/0,leaf/1,middle/1]).
run()->
    Old=erlang:system_flag(backtrace_depth,8),
    try
        lists:foreach(fun(Depth)->erlang:system_flag(backtrace_depth,Depth),check(Depth) end,[0,1,2,8]),done
    after erlang:system_flag(backtrace_depth,Old) end.
check(Depth)->
    try middle(bad) of _->error(missing_failure)
    catch error:Reason:Stack->
        badarg=Reason,
        [{erlang,hd,[bad],[{error_info,ErrorInfo}]}|Tail]=Stack,
        true=(ErrorInfo =:= #{module=>erl_erts_errors}),
        case Depth of 0->[]=Tail;1->[]=Tail;_-> [{generic_depth_hd,leaf,1,_}|_]=Tail end,
        true=(length(Stack)=<erlang:max(1,Depth))
    end.
middle(V)->R=generic_depth_hd:leaf(V),{middle_returned,R}.
leaf(V)->R=erlang:hd(V),{leaf_returned,R}.
