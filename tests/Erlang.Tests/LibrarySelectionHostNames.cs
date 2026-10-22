namespace Erlang.Tests;

internal static class LibrarySelectionHostNames
{
    internal const string Prefix = "runtime/library-selection/";
    internal const string ListsPrefix = Prefix + "lists/";
    internal const string QueuePrefix = Prefix + "queue/";
    internal const string Defaults = "defaults-and-explicit-managed";
    internal const string Both = "original-images-both-request-orders";
    internal const string Mixed = "explicit-mixed-dependency";
    internal const string Immutable = "owned-inputs-and-runtime-isolation";
    internal const string Refusals = "invalid-request-and-no-fallback";
    internal const string Reload = "no-bootstrap-generation-and-first-reload";
    internal const string Cancellation = "cancellation-and-post-start-token-lifetime";
    internal const string CallerModule = "library_selection_caller375";
    internal const string CallerRun = "run";
    internal const string CallerFile = CallerModule + ".erl";
    internal const string CallerSource = "-module(library_selection_caller375). -export([run/0]). run() -> lists:sort(queue:to_list(queue:from_list([3,1,2]))).";
    internal const string Failure = "Startup library selection check failed";
    internal const string Runtime = "erlang";
    internal const string Missing = "missing_library375";
}
