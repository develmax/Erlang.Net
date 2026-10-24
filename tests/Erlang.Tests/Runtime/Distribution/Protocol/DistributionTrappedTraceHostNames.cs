using Erlang;

namespace Erlang.Tests;

internal static class DistributionTrappedTraceHostNames
{
    internal const string Test = "runtime/distribution-trapped-canonical-trace-native-contract";
    internal const string Failure = "Canonical trapped exit trace contract failed";
    internal const string InitialLabel = "initial_label";
    internal const string IncomingLabel = "incoming_label";
    internal const string Reason = "selected_reason";
    internal const long ReceiveFlag = 2;
    internal const long Serial = 9;
    internal const long Last = 3;
    internal static readonly Atom Label = Term.A(IncomingLabel);
    internal static readonly Atom ExitReason = Term.A(Reason);
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
