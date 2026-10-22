namespace Erlang.Tests;

internal static class HamtNumericEqualityHostNames
{
    internal const string Test = "runtime/hamt-numeric-equality-own-layout-and-thread-scope";
    internal const string Module = "ordsets372_map_key_stub";
    internal const string Ordsets = "ordsets";
    internal const string IsEqual = "is_equal";
    internal const string EqualOperator = "==";
    internal const string Failure = "HAMT numeric equality did not preserve own VM traversal or comparison scope ownership";
    internal const int UniqueBytes = 16;
    internal const int HamtEntries = 33;
    internal const int FirstOldUnique = 1;
    internal const int SecondOldUnique = 2;
    internal const byte ThirdUnique = 64;
    internal const byte FourthUnique = 128;
}
