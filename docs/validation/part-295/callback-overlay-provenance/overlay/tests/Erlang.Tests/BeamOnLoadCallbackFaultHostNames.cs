namespace Erlang.Tests;

internal static class BeamOnLoadCallbackFaultHostNames
{
    internal const string Test = "host/BEAM-on-load-cleanup-fault-quiescence-and-retirement";
    internal const string InjectedFailure = "Expected on_load cancellation cleanup failure";
    internal const string Failure = "BEAM on_load callback cleanup fault contract failed";
    internal const string ResourceSuffix = "BeamOnLoad.fresh_success.new.beam_on_load_fresh_success.beam";
    internal const string Gate = "gate";
    internal const string Processes = "processes";
    internal const string Pending = "pendingBeamImages";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
