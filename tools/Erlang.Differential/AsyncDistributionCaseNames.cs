namespace Erlang.Differential;

public static class AsyncDistributionCaseNames
{
    public const string Prefix = "async-dist-";
    public const string ModulePrefix = "-module(oracle_async_dist). -export([run/0]). run() -> ";
    public const string ModuleSuffix = ".";
    public const string DefaultName = "default-false";
    public const string DefaultBody = "process_flag(async_dist, false)";
    public const string TransitionsName = "boolean-previous-values";
    public const string TransitionsBody = "{process_flag(async_dist,true),process_flag(async_dist,true),process_flag(async_dist,false),process_flag(async_dist,false)}";
    public const string IntegerName = "integer-value-badarg";
    public const string IntegerBody = "process_flag(async_dist, 1)";
    public const string AtomName = "invalid-atom-no-mutation";
    public const string AtomBody = "false=process_flag(async_dist,true), ok=try process_flag(async_dist,invalid_async_value) catch error:badarg -> ok end, process_flag(async_dist,false)";
    public const string ChildName = "child-does-not-inherit-parent-flag";
    public const string ChildBody = "false=process_flag(async_dist,true), Parent=self(), spawn(fun()->Parent!{async_child,process_flag(async_dist,false)} end), receive {async_child,V} -> {V,process_flag(async_dist,false)} after 5000 -> timeout end";
    public const string True = "true";
    public const string False = "false";
    public const string Error = "error";
    public const string BadArgument = "badarg";
}
