namespace Erlang.NifNativeBackend;

internal sealed record NativeWritableOwnerObservation(
    OrdinaryBinaryBacking Owner,
    long BinRef,
    nint Data,
    nuint Flags,
    nuint Apparent,
    long References,
    long Capacity
);
