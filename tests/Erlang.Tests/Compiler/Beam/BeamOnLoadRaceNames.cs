namespace Erlang.Tests;

internal static class BeamOnLoadRaceNames
{
    internal const string Gate = "gate";
    internal const string Processes = "processes";
    internal const string Pending = "pendingBeamImages";
    internal const string CodeImage = "CodeImage";
    internal const string Executing = "IsExecutingCode";
    internal const string Missing = "Pending callback race metadata is unavailable";
    internal const string LeaseNotReleased = "Callback did not release its actual image before outcome publication";
    internal const string OutcomeNotRecorded = "Callback did not record its actual return behind the runtime gate";
    internal const string UnexpectedPublication = "Killed callback was published from a cached ok return";
    internal const int WatchdogSeconds = 5;
}
