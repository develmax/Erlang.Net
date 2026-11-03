namespace Erlang.NifNativeBackend;

internal sealed record NativeWritableHeaderObservation(
    long Identity,
    long BinRef,
    nuint Base,
    nuint Start,
    nuint End
);
