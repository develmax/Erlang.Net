namespace Erlang.Tests;

internal static class CancellationFailure669Names
{
    internal const string Case = "runtime/payload669/cancellation-failure-still-cancels-real-source-children";
    internal const string Module = "cancel_failure669";
    internal const string Start = "start";
    internal const string SourceFile = "cancel_failure669.erl";
    internal const string Ready = "ready";
    internal const string Input = "VmSpawnInput641";
    internal const string Unwound = "Unwound";
    internal const string Processes = "processes";
    internal const string Context = "Context641";
    internal const string Ended = "ended";
    internal const string Marker = "Actual host cancellation callback669 failed.";
    internal const string Failure = "Cancellation failure orphaned actual source children or their payload641 work.";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
    internal const string Source = """
        -module(cancel_failure669).
        -export([start/1]).
        start(Parent) ->
          [spawn(fun() -> Parent!{ready,self()},receive stop -> ok end end),
           spawn(fun() -> Parent!{ready,self()},receive stop -> ok end end)].
        """;
}
