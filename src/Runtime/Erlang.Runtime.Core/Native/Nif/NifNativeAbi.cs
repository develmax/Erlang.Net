namespace Erlang;

internal static class NifNativeAbi
{
    internal const int PointerBytes = 8;
    internal const int EntryBytes = 96;
    internal const int EntryVersionPrefixBytes = 8;
    internal const int BridgeTableBytes = 368;
    internal const int FunctionBytes = 32;
    internal const int MajorVersion = 2;
    internal const int MinorVersion = 18;
    internal const uint InitializerEntryOptions = 1;
    internal const int Latin1Encoding = 1;
    internal const int StringReadBoundary = 4096;
    internal const int HashHexCharacters = 64;
    internal const int EnvironmentStorageBytes = 1;
}
