namespace Erlang.NifNativeBackend;

internal sealed record NativeWritableObservation(
    IReadOnlyList<NativeWritableOwnerObservation> Owners,
    IReadOnlyList<NativeWritableHeaderObservation> Headers,
    IReadOnlyList<long> Regular,
    IReadOnlyList<long> Writable,
    long PhysicalWords
);
