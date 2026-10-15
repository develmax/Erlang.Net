using Erlang;

namespace Erlang.Tests;

internal static class DistributionTraceHostNames
{
    internal const string Test = "runtime/distribution-trace-selection-and-owned-signals";
    internal const string Failure = "Distribution trace assertion failed";
    internal const string Local = "trace_local@localhost";
    internal const string Remote = "trace_remote@localhost";
    internal const string Registered = "trace_recipient";
    internal const string Payload = "trace_payload";
    internal const string Skipped = "trace_skipped";
    internal const string InitialLabel = "trace_initial";
    internal const string IncomingLabel = "trace_incoming";
    internal const string Preserve = "undefined";
    internal const string ArbitraryToken = "outside_asserted_shape";
    internal const string Reason = "trace_remote_exit";
    internal const long ReceiveFlag = 2;
    internal const long WideUnknownFlag = 1L << 40;
    internal const long Serial = 9;
    internal const long Last = 3;
    internal const uint MissingId = 900000;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
    internal static readonly Atom Label = Term.A(IncomingLabel);
    internal static readonly Atom Message = Term.A(Payload);
}
