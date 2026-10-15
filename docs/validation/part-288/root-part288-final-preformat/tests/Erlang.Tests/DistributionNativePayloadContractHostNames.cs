using Erlang;

namespace Erlang.Tests;

internal static class DistributionNativePayloadContractHostNames
{
    internal const string Test = "runtime/distribution-original-payload14-direct-contract-regressions";
    internal const string Failure = "Original payload signal contract failed";
    internal const string PayloadLocal = "payload_local@localhost";
    internal const string PayloadRemote = "payload_remote@localhost";
    internal const string MonitorLocal = "signal_local@localhost";
    internal const string MonitorRemote = "signal_remote@localhost";
    internal const string AcceptedName = "ok";
    internal const string InitialLabelName = "initial_payload_label";
    internal const string PayloadLabelName = "payload_label";
    internal const string PayloadReasonName = "payload_reason";
    internal const string ReasonHeadName = "a";
    internal const string ReasonTailName = "binary_tail";
    internal const int PayloadRemoteId = 1;
    internal const int MonitorRemoteId = 11;
    internal const uint RequestedLocalCreation = 1;
    internal const uint RemoteCreation = 1;
    internal const uint MissingLocalId = 900000;
    internal const long ReceiveFlag = 2;
    internal const long Serial = 9;
    internal const long LastCount = 3;
    internal const int SingleOutputSegment = 1;
    internal const byte EtfVersion = 131;
    internal const byte InvalidEtfTag = 255;
    internal const byte TrailingReasonByte = 0;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
    internal static readonly Atom Accepted = Term.A(AcceptedName);
    internal static readonly Atom InitialLabel = Term.A(InitialLabelName);
    internal static readonly Atom PayloadLabel = Term.A(PayloadLabelName);
    internal static readonly Atom PayloadLocalNode = Term.A(PayloadLocal);
    internal static readonly Atom PayloadRemoteNode = Term.A(PayloadRemote);
    internal static readonly Atom MonitorLocalNode = Term.A(MonitorLocal);
    internal static readonly Atom MonitorRemoteNode = Term.A(MonitorRemote);
    internal static readonly Term Reason = Term.Tuple(
        Term.A(PayloadReasonName),
        new BitString(new byte[] { 0, 128, 255 }),
        new Cons(Term.A(ReasonHeadName), Term.A(ReasonTailName))
    );
}
