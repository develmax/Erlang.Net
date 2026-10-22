namespace Erlang;

internal static class DebugLockNames419
{
    internal const string Module = "erts_debug", Clear = "lcnt_clear", Collect = "lcnt_collect", Control = "lcnt_control", LockCounting = "lock_counting", Error = "error";
    internal const string Foreign = "Lock-counting-disabled native profile requires its exact live owning runtime", WrongProfile = "Runtime has an active lock-counting backend";
    internal static readonly (string Function, int Arity)[] Signatures = [(Clear, 0), (Collect, 0), (Control, 1), (Control, 2)];
    // This runtime build contains no lock counters or lock-count instrumentation.
    internal const bool BuildHasLockCounting = false;
}
