using System.Reflection;

namespace Erlang.Tests;

internal static class MailboxRootRetirement593Names
{
    internal const string Owner = "Erlang.RuntimeMailboxRootOwner593";
    internal const string Work = "Erlang.MailboxPayloadLease593";
    internal const string Tracker = "Erlang.MailboxRetirementTracker593";
    internal const string Active = "active";
    internal const string Link = "Link";
    internal const string AddLast = "AddLast";
    internal const string Attach = "Attach";
    internal const string BindRecipient = "BindRecipient";
    internal const string References = "references";
    internal const string OwnerReleased = "ownerReleased";
    internal const string Ready = "ready";
    internal const string QueueSettled = "queueSettled";
    internal const string QueueFailure = "schedulingFailure";
    internal const string Close = "Close";
    internal const string Drain = "DrainAsync";
    internal const string QueueMarker = "simulated post-queue failure boundary";
    internal const string ReleaseMarker = "actual finite root-release failure";
    internal const string MissingFailure = "Root-release tracker lost a scheduling/release failure.";
    internal const string DisposedCount = "Root-release work must execute exactly once.";
    internal const string UnrelatedWork = "Recipient drain touched another recipient's retained root.";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
