-module(ordsets_core_support).
-export([run/2,profiles/2]).
run(Module,Names) -> [{Name,value(Module,Name)} || Name <- Names].
value(Module,Name) ->
    try erlang:apply(Module,probe,[Name]) of Value -> {ok,Value}
    catch Class:Reason -> {error,Class,Reason} end.
profiles(Module,Names) ->
    Default = {default,[{Name,capture(Module,Name)} || Name <- Names]},
    Old = erlang:system_flag(backtrace_depth,8),
    try [Default | [at_depth(Depth,Module,Names) || Depth <- [0,1,2,8]]]
    after erlang:system_flag(backtrace_depth,Old) end.
at_depth(Depth,Module,Names) ->
    erlang:system_flag(backtrace_depth,Depth),
    {Depth,[{Name,capture(Module,Name)} || Name <- Names]}.
capture(Module,Name) ->
    try erlang:apply(Module,probe,[Name]) of Value -> {ok,Value}
    catch Class:Reason:Stack -> {error,Class,Reason,Stack} end.
