namespace Erlang.Tests;

internal static class StaticApplyRegressionSources
{
    internal const string ExecutionFile = "static_apply368_execution.erl";
    internal const string ImportedFile = "static_apply368_imported.erl";
    internal const string ShadowFile = "static_apply368_shadow.erl";
    internal const string OtherFile = "static_apply368_foreign.erl";
    internal const string SuppressedFile = "static_apply368_suppressed.erl";
    internal const string Execution = """
-module(static_apply368_execution).
-export([run/0,pair/2]).
run() ->
    Old = erlang:system_flag(backtrace_depth,8),
    try
        put(order,[]),
        Pair = erlang:apply(static_apply368_execution,pair,[mark(a),mark(b)]),
        Order = lists:reverse(erase(order)),
        Auto = apply(queue,new,[]),
        Tail = capture(fun tail/0),
        NonTail = capture(fun non_tail/0),
        Improper = capture(fun improper/0),
        Dynamic = capture(fun dynamic/0),
        {Pair,Order,Auto,Tail,NonTail,Improper,Dynamic}
    after erlang:system_flag(backtrace_depth,Old) end.
pair(A,B) -> {A,B}.
mark(Value) -> put(order,[Value|get(order)]),Value.
capture(Function) ->
    try Function() of Value -> {ok,Value} catch Class:Reason:Stack -> {Class,Reason,Stack} end.
tail() -> erlang:apply(queue,get,[bad]).
non_tail() ->
    Value = erlang:apply(queue,get,[bad]),
    {after_call,Value}.
improper() -> erlang:apply(queue,new,[a|argument_tail]).
dynamic() -> Arguments = [a|argument_tail], erlang:apply(queue,new,Arguments).
""";
    internal const string Imported = """
-module(static_apply368_imported).
-export([run/0]).
-compile({no_auto_import,[apply/3]}).
-import(erlang,[apply/3]).
run() -> apply(queue,new,[]).
""";
    internal const string Shadow = """
-module(static_apply368_shadow).
-export([run/0]).
-compile({no_auto_import,[apply/3]}).
run() -> apply(queue,new,[]).
apply(_,_,_) -> shadow.
""";
    internal const string Other = """
-module(static_apply368_foreign).
-export([run/0]).
-compile({no_auto_import,[apply/3]}).
-import(static_apply368_other,[apply/3]).
run() -> apply(queue,new,[]).
""";
    internal const string Suppressed = """
-module(static_apply368_suppressed).
-export([run/0]).
-compile({no_auto_import,[apply/3]}).
run() -> apply(queue,new,[]).
""";
}
