namespace Erlang.Tests;

internal static class ProcessInfoLate601Names
{
    internal const string PhaseObservation = "snapshot601 late phase: {0}";
    internal const string ReplyReturnedPhase = "reply-returned";
    internal const string DestinationAdoptedPhase = "destination-adopted";
    internal const string RequesterCollectedPhase = "requester-major-collected";
    internal const string DestinationRetirementPhase = "destination-retirement";
    internal const string CancelCase = "runtime/mailbox601/cancel-after-native-reply-publication-and-target-exit";
    internal const string ExitBeforeCase = "runtime/mailbox601/target-exit-before-capture-replies-undefined";
    internal const string ExitAfterCase = "runtime/mailbox601/target-done-before-requester-consumes-native-reply";
    internal const string Suspend = "suspend_process";
    internal const string Resume = "resume_process";
    internal const string Asynchronous = "asynchronous";
}
