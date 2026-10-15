namespace Erlang.NifNativeBackend;

internal sealed record NifNativeReallocationCase(
    string Name,
    int Selector,
    int ExpectedMask,
    int[] RequestedSizes
);
