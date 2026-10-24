namespace Erlang.Tests;

internal static class DistributionOutputAdmissionHostNames
{
    internal const string Test = "runtime/distribution-output-reservation-cancellation-oversize-and-retirement";
    internal const string Local = "output_admission_local@localhost";
    internal const string Remote = "output_admission_remote@localhost";
    internal const string Filler = "admission_filler";
    internal const string First = "admission_first";
    internal const string Cancelled = "admission_cancelled";
    internal const string Follower = "admission_follower";
    internal const string Replacement = "admission_replacement";
    internal const string GateField = "gate";
    internal const string ParametersType = "Erlang.DistributionConnectedParameters";
    internal const string PacketBudgetField = "MaximumQueuedPacketBytes";
    internal const string Failure = "Distribution output admission contract failed";
    internal const uint Creation = 1;
    internal const ulong RemoteId = 77;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
