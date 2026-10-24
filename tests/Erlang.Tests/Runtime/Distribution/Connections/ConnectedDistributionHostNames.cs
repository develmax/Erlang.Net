namespace Erlang.Tests;

internal static class ConnectedDistributionHostNames
{
    internal const string Test = "runtime/connected-distribution-controller-queue-lifecycle-node-events";
    internal const int ExpectedAssertions = 132;
    internal const string Local = "connected_local@localhost";
    internal const string Remote = "connected_remote@localhost";
    internal const string ControllerLookup = "RequireDistributionControllerTarget";
    internal const string Admission = "AdmitObservedLocalNode";
    internal const string Kernel = "net_kernel";
    internal const string Sink = "connected_sink";
    internal const string Payload = "connected_payload";
    internal const string Notice = "dist_ctrlr";
    internal const string Data = "dist_data";
    internal const string Success = "ok";
    internal const string Failure = "Connected foundation assertion failed";
    internal const string FunctionModule = "connected_closure";
    internal const string SelfFunction = "self";
    internal const int FunctionIndex = 7;
    internal const int FunctionOldUnique = 13;
    internal const int FunctionArity = 1;
    internal const int FunctionEnvironmentCount = 1;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
