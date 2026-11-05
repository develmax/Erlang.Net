namespace Erlang;

internal static class AtomicsOwnership586Names
{
    internal static ulong BinaryPrefixBytes => (ulong)NativeMagicLayout.BinaryDataOffset;
    internal static ulong DataOffsetBytes => (ulong)NativeMagicLayout.AlignedDataOffset;
    internal const string Foreign = "Atomics backing belongs to a different runtime or lease.";
    internal const string Retired = "The actual atomics magic backing has retired.";
    internal const string Underflow = "Atomics backing ownership lease underflow.";
    internal const string Identity = "Atomics magic backing identity has not been bound.";
    internal const string InvalidPrefix = "The actual atomics binary prefix does not match its admitted allocation.";
}
