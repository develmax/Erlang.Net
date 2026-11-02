namespace Erlang;

internal static class RuntimeWorkerStartupNames
{
    internal const string DispatcherGate = "Normal worker startup cannot hold the dispatcher gate";
    internal const string DispatcherRollbackGate = "Normal worker rollback cannot hold the dispatcher gate";
    internal const string RuntimeGate = "VM worker bootstrap cannot hold the runtime gate";
    internal const string RuntimeRollbackGate = "Runtime constructor rollback cannot hold the runtime gate";
    internal const string NotReady = "Normal workers are not ready";
}
