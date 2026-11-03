using System.Reflection;

namespace Erlang.Tests;

internal static class MailboxSnapshotSettlement593Names
{
    internal const string Owner = "Erlang.RuntimeMailboxRootOwner593";
    internal const string Work = "Erlang.MailboxPayloadLease593";
    internal const string Tracker = "Erlang.MailboxRetirementTracker593";
    internal const string Snapshot = "Erlang.MailboxGraphSnapshot593";
    internal const string Settlement = "Erlang.MailboxRootSettlement593";
    internal const string Active = "active";
    internal const string Link = "Link";
    internal const string AddLast = "AddLast";
    internal const string Attach = "Attach";
    internal const string Fork = "ForkForRecipient";
    internal const string AdmittedProperty = "Admitted";
    internal const string References = "references";
    internal const string OwnerReleased = "ownerReleased";
    internal const string Ready = "ready";
    internal const string QueueSettled = "queueSettled";
    internal const string QueueFailure = "schedulingFailure";
    internal const string Drain = "DrainAsync";
    internal const string Settle = "SettleAsync";
    internal const string OtherRecipient = "Nonfinal recipient waited or released another recipient's roots.";
    internal const string FinalRoot = "Final recipient did not settle its actual typed-root disposal.";
    internal const string FailedFallback = "Specific root fallback lost failure or did not dispose actual roots.";
    internal const string Marker = "privileged Ready root-work scheduling failure";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
