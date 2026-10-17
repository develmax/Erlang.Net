namespace Erlang.NifNativeBackend;
internal static class NifOrdinaryLoaderNames
{
    internal const string Failure = "Ordinary loader OS regression control failed.";
    internal const string Node = "ordinary_loader@fixture";
    internal const string Module = "ordinary_loader_probe";
    internal const string Load = "load";
    internal const string Probe = "entryprobe";
    internal const string Ok = "ok";
    internal const string Bridge = "Erlang.NifBridge.so";
    internal const string LoadTerm = "load.etf";
    internal const string ReturnTerm = "returned.etf";
    internal const string ErrorTerm = "error.etf";
    internal const string Completion = "completion.json";
    internal const string Acquired = "ACQUIRED_ORDINARY_OS_LOADER_CONTROL";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(60);
}
