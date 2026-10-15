using Erlang;

namespace Erlang.Tests;

internal static class DistributionSenderHostNames
{
    internal const string Test = "runtime/distribution-sender-payload-selection-and-owned-ingress";
    internal const string Failure = "Distribution sender assertion failed";
    internal const string Local = "sender_host_local@localhost";
    internal const string Remote = "sender_host_remote@localhost";
    internal const string Third = "sender_host_third@localhost";
    internal const string Skipped = "sender_skipped";
    internal const string Selected = "sender_selected";
    internal const string LabelName = "sender_label";
    internal const string InitialLabel = "sender_initial_label";
    internal const string Preserve = "undefined";
    internal const string Kernel = "net_kernel";
    internal const string SetNode = "setnode";
    internal const long WideFlag = 1L << 40;
    internal const long Serial = 9;
    internal const long Last = 3;
    internal const uint MissingId = 900000;
    internal static readonly Atom Label = Term.A(LabelName);
    internal static readonly Atom First = Term.A(Skipped);
    internal static readonly Atom Second = Term.A(Selected);
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
