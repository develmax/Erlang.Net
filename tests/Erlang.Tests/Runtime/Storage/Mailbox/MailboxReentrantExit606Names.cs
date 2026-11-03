using System.Reflection;

namespace Erlang.Tests;

internal static class MailboxReentrantExit606Names
{
    internal const string PendingCase = "runtime/mailbox606/pending-admission-self-exit-normal-and-bounded";
    internal const string DeferredCase = "runtime/mailbox606/deferred-release-self-exit-complete-and-discard-once";
    internal const string LateCase = "runtime/mailbox606/late-deferred-send-after-owner-done-releases-outside-monitor";
    internal const string Failure = "Reentrant mailbox termination reused a retired node or lost exact release ownership.";
    internal const string CleanupFailure = "Mailbox reentrant termination assertion and independent cleanup failed.";
    internal const string Mailbox = "Mailbox";
    internal const string Snapshot = "Snapshot";
    internal const string RuntimeGate = "gate";
    internal const string DeferredGateHeld = "DeferredGateHeld";
    internal const string Pending = "pending";
    internal const string DeferredPending = "deferredPending";
    internal const string Admitted = "AdmittedCount593";
    internal const string HasPendingSignals = "HasPendingSignals428";
    internal const string HasDeferredPending = "HasDeferredPending";
    internal const string Kill = "kill";
    internal const string Killed = "killed";
    internal const string SendPending = "SendPendingSnapshot593";
    internal const string AdmitPending = "AdmitPending";
    internal const string AdmitBounded = "AdmitPendingBounded428";
    internal const string SendDeferred = "SendDeferred";
    internal const string ClaimDeferred = "ClaimDeferred";
    internal const string DiscardPending = "DiscardPending";
    internal const string Prepare = "Prepare";
    internal const string Complete = "Complete";
    internal const string DeferredAdmissionType = "Erlang.DeferredMailboxAdmission";
    internal const int SignalBudget = 1;
    internal const int ExistingMessage = 606;
    internal const int RequestedCopies = 3;
    internal const int CloseAtCopy = 2;
    internal const string QueueLengthObserver = "QueueLengthChanged";
    internal const string StopForHalt = "StopForHalt";
    internal const string Copies = "Copies";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(20);
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
}
