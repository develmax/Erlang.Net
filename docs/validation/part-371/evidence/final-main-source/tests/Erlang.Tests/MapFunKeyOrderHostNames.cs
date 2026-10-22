namespace Erlang.Tests;

internal static class MapFunKeyOrderHostNames
{
    internal const string TestName = "terms/map-fun-key-order-and-deep-comparison-scopes";
    internal const string Module = "map_fun_key_order_host";
    internal const string Failure = "Map ordering followed canonical FUN key storage instead of native ordering, lost exact keys, or changed deep comparison semantics.";
    internal const int FunctionUniqueBytes = 16;
    internal const int DeepWrappers = 32768;
    internal const byte ReversedFirstUnique = 255;
    internal const byte ReversedSecondUnique = 0;
    internal const byte TiedDistinctUnique = 128;
    internal const int FirstOldUnique = 1;
    internal const int SecondOldUnique = 2;
}
