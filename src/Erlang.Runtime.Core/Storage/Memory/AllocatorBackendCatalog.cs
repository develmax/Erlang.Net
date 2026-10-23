using System.Collections.Frozen;

namespace Erlang;

internal static class AllocatorBackendCatalog
{
    // Exact OTP29.1.1 erl_alloc.types descriptors plus erl_alloc.c special names.
    internal static IReadOnlySet<string> RecognizedNames
    {
        get;
    } = new[]
    {
        AllocatorBackendNames.System,
        AllocatorBackendNames.Temporary,
        AllocatorBackendNames.ShortLived,
        AllocatorBackendNames.Standard,
        AllocatorBackendNames.LongLived,
        AllocatorBackendNames.ProcessHeap,
        AllocatorBackendNames.Ets,
        AllocatorBackendNames.FixedSize,
        AllocatorBackendNames.Literal,
        AllocatorBackendNames.Binary,
        AllocatorBackendNames.Driver,
        AllocatorBackendNames.Test,
        AllocatorBackendNames.Mseg,
        AllocatorBackendNames.Mapped,
        AllocatorBackendNames.Utility,
    }.ToFrozenSet(StringComparer.Ordinal);

    internal static AllocatorBackendCapability Capability(string name)
    {
        if (!RecognizedNames.Contains(name))
            throw new InvalidOperationException(SystemAllocatorQueryDiagnostics.UnrecognizedCatalogName);

        return name == AllocatorBackendNames.System
            ? AllocatorBackendCapability.Supported
            : AllocatorBackendCapability.RecognizedUnsupported;
    }
}
