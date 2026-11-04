namespace Erlang.Tests;

internal static class BulkMapSourceNames
{
    internal const string Case = "runtime/maps-from-list-bulk-physical-occurrences";
    internal const string Resource = "Erlang.Tests.OrdsetsBulkSource.inputs-and-original.etf";
    internal const string Failure = "Bulk map source occurrence contract failed";
    internal const string Mixed = "bulk_33";
    internal const string Identical = "bulk_exact_33";
    internal const string Interleaved = "bulk_repeat_33";
    internal const int InputCount = 33;
    internal const int MinimumInterleavedCount = 25;
    internal const int WaitSeconds = 30;
}
