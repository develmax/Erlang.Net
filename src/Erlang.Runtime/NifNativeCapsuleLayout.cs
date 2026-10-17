namespace Erlang;

internal static class NifNativeCapsuleLayout
{
    internal const int HeaderBytes = 64;
    internal const int ProgramHeaderBytes = 56;
    internal const int ProgramHeaders = 3;
    internal const int SymbolBytes = 24;
    internal const int HashBytes = 16;
    internal const int DynamicEntryBytes = 16;
    internal const int FixedDynamicEntries = 6;
    internal const int Alignment = 8;
    internal const ulong PageAlignment = 4096;
    internal const ushort SharedObject = 3;
    internal const ushort X64Machine = 62;
    internal const uint Version = 1;
    internal const uint LoadSegment = 1;
    internal const uint DynamicSegment = 2;
    internal const uint StackSegment = 0x6474e551;
    internal const ulong StackAlignment = 16;
    internal const uint ReadWrite = 6;
    internal const ulong Needed = 1;
    internal const ulong Hash = 4;
    internal const ulong StringTable = 5;
    internal const ulong SymbolTable = 6;
    internal const ulong StringBytes = 10;
    internal const ulong SymbolEntryBytes = 11;
    internal static readonly byte[] Identification = [127, 69, 76, 70, 2, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0];
}
