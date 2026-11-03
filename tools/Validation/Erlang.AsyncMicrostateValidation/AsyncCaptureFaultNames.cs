namespace Erlang.Validation.AsyncMicrostates;

internal static class AsyncCaptureFaultNames
{
    internal const string PackageManifestProperty = "PackageManifest";
    internal const string ContractFailureMessage = "Actual admitted child capture fault contract failed";
    internal const string MissingCaptureMessage = "Actual ready child capture missing";
    internal const string FrontendChildField = "child";
    internal const string ChildProcessField = "process";
    internal const string RuntimeGateField = "gate";
    internal const string FrontendLeaseCountField = "asyncAccountingLeases1170";
    internal const string ChildExecutableName = "Erlang.DriverHost.exe";
    internal const string ChildExitMode = "owned-child-exit-after-capture";
    internal const string CaptureMetadataField = "metadata";
    internal const string Scope = "Actual authenticated component capture-client failure only; no public statistics/caller cancellation/held native job/OTP parity claim";
    internal const string WrongEpochMode = "wrong-epoch-after-capture";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
    internal const int AsyncWorkers = 2;
    internal const int InitialCaptureEpoch = 1;
    internal const int SingleFrontendLease = 1;
    internal const int WrongEpochIncrement = 1;
}
