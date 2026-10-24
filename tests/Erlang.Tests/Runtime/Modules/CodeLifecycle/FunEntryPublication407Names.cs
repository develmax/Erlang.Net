using System.Reflection;

namespace Erlang.Tests;

internal static class FunEntryPublication407Names
{
    internal const string HistoryTest = "runtime/library407/fun-entry-load-purge-without-construction-history";
    internal const string PreflightTest = "runtime/library407/fun-entry-prepare-and-publication-preflight-boundaries";
    internal const string ConcurrencyTest = "runtime/library407/fun-entry-publication-safe-external-lock-order";
    internal const string Failure = "Canonical function entry publication control failed";
    internal const string EntriesField = "entries";
    internal const string MetadataField = "funLegacyEntries";
    internal const string Length = "length";
    internal const int ForgedOldUnique = 1;
    internal const int ForgedOldIndex = 255;
    internal const int Iterations = 16;
    internal const int ExternalAttempts = 128;
    internal const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
}
