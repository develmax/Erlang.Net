namespace Erlang;

internal static class AtomicsOwnership586Names
{
    internal const ulong BinaryPrefixBytes = 32;
    internal const ulong DataOffsetBytes = 56;
    internal const string Foreign = "Atomics backing belongs to a different runtime or lease.";
    internal const string Retired = "The actual atomics magic backing has retired.";
    internal const string Underflow = "Atomics backing ownership lease underflow.";
    internal const string Identity = "Atomics magic backing identity has not been bound.";
    internal const string InvalidPrefix = "The actual atomics binary prefix does not match its admitted allocation.";
}
