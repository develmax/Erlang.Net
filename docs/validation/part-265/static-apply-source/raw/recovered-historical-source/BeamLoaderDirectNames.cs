namespace Erlang.Tests;

internal static class BeamLoaderDirectNames
{
    internal const string PendingTable = "loaderPendingImages";
    internal const string ImageProperty = "CodeImage";
    internal const string ExecutingMethod = "IsExecutingCode";
    internal const string NativeWrapperResource = "loader_direct_caller.beam";
    internal const string Callback = "call_on_load_function";
    internal const string Failure = "Direct loader callback contract failed";
    internal const string Source = "-module(loader_candidate_direct). -export([direct/1,dynamic/3]). direct(M)->erlang:apply(erlang,call_on_load_function,[M]). dynamic(Mod,Fun,Args)->erlang:apply(Mod,Fun,Args).";
    internal const string Module = "loader_candidate_direct";
    internal const string Direct = "direct";
    internal const string Dynamic = "dynamic";
    internal const string FreshResource = "fresh_success.new.beam_on_load_fresh_success.beam";
    internal const string ReplacementOldResource = "replacement_failure.old.beam_on_load_replace_failure.beam";
    internal const string ReplacementNewResource = "replacement_failure.new.beam_on_load_replace_failure.beam";
    internal const string ExpectedCallbackFailure = "on_load_rejected";
    internal const int WatchdogSeconds = 10;
}
