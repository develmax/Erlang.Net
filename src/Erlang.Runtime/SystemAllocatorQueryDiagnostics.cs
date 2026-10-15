namespace Erlang;

internal static class SystemAllocatorQueryDiagnostics
{
    internal const string DifferentRuntime = "System allocator query belongs to another runtime.";
    internal const string UnrecognizedCatalogName = "Allocator backend lookup requires a validated pinned name.";
    internal const string InvalidQueryKind = "Unknown system allocator query kind.";
}
