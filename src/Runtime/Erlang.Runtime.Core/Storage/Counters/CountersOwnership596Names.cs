namespace Erlang;

internal static class CountersOwnership596Names
{
    internal const ulong BinaryPrefixBytes = CountersLayout591Names.BinaryPrefixBytes;
    internal const ulong DataOffsetBytes = CountersLayout591Names.MagicDataOffset;
    internal const string Foreign = "Counters backing belongs to a different runtime or lease.";
    internal const string Retired = "The actual counters magic backing has retired.";
    internal const string Underflow = "Counters backing ownership lease underflow.";
    internal const string Identity = "Counters magic backing identity has not been bound.";
    internal const string InvalidPrefix = "The actual counters binary prefix does not match its admitted allocation.";
}
