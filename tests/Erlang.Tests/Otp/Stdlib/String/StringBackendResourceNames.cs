namespace Erlang.Tests;

internal static class StringBackendResourceNames
{
    internal const int InputBytes = 16 * 1024;
    internal const long AllocationBudget = 32L * 1024 * 1024;
    internal const int BitsPerByte = 8;
    internal const string Length = "length";
    internal const string Failure = "Managed string length must avoid copying every binary tail; allocated bytes: ";
    internal const string LengthFailure = "Large binary string length differs from its codepoint count";
}
