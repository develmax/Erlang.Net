namespace Erlang.Tests;

internal static class InternalPortOrder416Names
{
    internal const string BadFlagsTest = "HOST/internal-ports416/bad-flags-before-real-blocked-command-queue";
    internal const string ConnectTest = "HOST/internal-ports416/connect-after-real-queued-close-and-invalid-target-order";
    internal const string Failure = "Internal port416 source ordering control failed";
    internal const string InvalidFlags = "invalid_internal_flags416", InvalidPid = "invalid_internal_pid416";
    internal const string SourceFile = "erts/emulator/beam/erl_bif_port.c", BackendFile = "erts/emulator/beam/io.c";
}
