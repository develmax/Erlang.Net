-module(generic_depth_observe).
-export([run/0,error_leaf/1,error_middle/1,hd_leaf/1,hd_middle/1]).
run()->
    Old=erlang:system_flag(backtrace_depth,8),
    try [{Depth,observe(Depth,error),observe(Depth,hd)} || Depth<-[0,1,2,8]]
    after erlang:system_flag(backtrace_depth,Old) end.
observe(Depth,Kind)->
    erlang:system_flag(backtrace_depth,Depth),
    try case Kind of error->error_middle(probe_reason);hd->hd_middle(bad) end
    catch Class:Reason:Stack->{Class,Reason,Stack} end.
error_middle(V)->R=error_leaf(V),{middle_returned,R}.
error_leaf(V)->R=erlang:error(V),{leaf_returned,R}.
hd_middle(V)->R=hd_leaf(V),{middle_returned,R}.
hd_leaf(V)->R=erlang:hd(V),{leaf_returned,R}.
