namespace Erlang.Tests;

internal static class UnicodeBackendNames
{
    internal const string ManagedTest = "runtime/unicode/managed-selection-and-original-contracts";
    internal const string BeamTest = "runtime/unicode/original-selection-and-original-contracts";
    internal const string RefusalTest = "runtime/unicode/explicit-dependencies-and-no-fallback";
    internal const string Failure = "Unicode backend differs from pinned OTP";
    internal const string ResourcePrefix = "Erlang.Tests.UnicodeBackend.";
    internal const string Observer = "unicode_port_cases";
}
