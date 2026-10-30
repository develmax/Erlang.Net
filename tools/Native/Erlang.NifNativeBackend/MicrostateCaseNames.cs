namespace Erlang.NifNativeBackend;

public static class MicrostateCaseNames
{
    public const string Feature = "runtime/statistics-msacc-default-feature-reset-and-caller-exit";
    public const string Producers = "runtime/statistics-msacc-actual-normal-aux-dirty-producers";
    public const string Nested = "runtime/statistics-msacc-real-thread-nested-vm-and-host-exclusion";
    public const string Cache = "runtime/statistics-msacc-thread-cache-does-not-retain-vm";
    internal const string Type = "type";
    internal const string Identity = "id";
    internal const string Counters = "counters";
    internal const string Reset = "reset";
    internal const string Emulator = "emulator";
    internal const string Aux = "aux";
    internal const string Sleep = "sleep";
    internal static readonly string[] Keys = ["aux", "check_io", "emulator", "gc", "other", "port", "sleep"];
}
