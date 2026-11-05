namespace Erlang;

internal static class PrimFileStorageLayout
{
    internal const int IndirectHeaderBytes = 8;
    internal const int IndirectPointerOffset = 4;
    internal const long MaximumIndirectReadBytes = 1L << 31;
}
