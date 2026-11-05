namespace Erlang;

internal static class NifNativeAbi
{
    internal const int Pointer32Bytes = sizeof(uint);
    internal const int Pointer64Bytes = sizeof(ulong);
    internal static int PointerBytes => IntPtr.Size;
    internal static int EntryBytes => IntPtr.Size == Pointer32Bytes ? 52 : 96;
    internal const int EntryVersionPrefixBytes = 8;
    internal static int BridgeTableBytes => 2 * sizeof(uint) + 61 * IntPtr.Size;
    internal static int BinaryDescriptorBytes => 5 * IntPtr.Size;
    internal static int FunctionBytes => IntPtr.Size == Pointer32Bytes ? 16 : 32;
    internal const int MajorVersion = 2;
    internal const int MinorVersion = 18;
    internal const uint InitializerEntryOptions = 1;
    internal const int Latin1Encoding = 1;
    internal const int StringReadBoundary = 4096;
    internal const int HashHexCharacters = 64;
    internal const int EnvironmentStorageBytes = 1;
}
