namespace Erlang.Tests;

internal static class UnicodeBackendNames
{
    internal const string ManagedTest = "runtime/unicode/managed-selection-and-original-contracts";
    internal const string BeamTest = "runtime/unicode/original-selection-and-original-contracts";
    internal const string RefusalTest = "runtime/unicode/explicit-dependencies-and-no-fallback";
    internal const string GrowthTest = "runtime/unicode/large-integer-list-linear-allocation-and-append-backing";
    internal const string GrowthObserver = "unicode_growth_cases";
    internal const string GrowthFailure = "Unicode large conversion value, append backing or allocation scaling differs";
    internal const int AllocationSmallCount = 10000;
    internal const int AllocationLargeCount = 20000;
    internal const long AllocationNoiseBytes = 1024 * 1024;
    internal const int MaximumAllocationDoublingFactor = 3;
    internal const string Failure = "Unicode backend differs from pinned OTP";
    internal const string ResourcePrefix = "Erlang.Tests.UnicodeBackend.";
    internal const string Observer = "unicode_port_cases";
}
