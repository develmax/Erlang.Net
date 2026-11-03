namespace Erlang;

internal readonly record struct RuntimeEtsStoredNode(
    RuntimeEtsStoredKind Kind,
    byte[]? Bytes = null,
    int BitLength = 0,
    long FloatBits = 0,
    int[]? Children = null,
    string[]? Names = null,
    bool Exported = false,
    RuntimeExternalTermReservation? Identity = null,
    IRuntimeStoredFunctionRecipe? Function = null,
    PersistentBinarySlice416? NativeBinary524 = null
);
