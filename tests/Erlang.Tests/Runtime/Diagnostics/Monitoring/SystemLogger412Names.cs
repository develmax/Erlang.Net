namespace Erlang.Tests;

internal static class SystemLogger412Names
{
    internal const string ValuesTest = "runtime/system-logger412/atomic-oldvalue-and-per-runtime-state";
    internal const string IdentityTest = "runtime/system-logger412/dead-own-foreign-and-remote-pid";
    internal const string InvalidTest = "runtime/system-logger412/invalid-value-preserves-state-and-other-flags";
    internal const string Erlang = "erlang", Set = "system_flag", Info = "system_info", Flag = "system_logger";
    internal const string Logger = "logger", Undefined = "undefined", Invalid = "logger412_target";
    internal const string Backtrace = "backtrace_depth", ForeignNode = "logger412@remote";
    internal const int RemoteId = 123, RemoteCreation = 7;
    internal const string Failure = "System logger native state/source rules differ";
}
