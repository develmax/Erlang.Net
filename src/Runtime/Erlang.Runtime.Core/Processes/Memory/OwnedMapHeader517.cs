namespace Erlang;

// Actual out-of-band structural state consumed by payload views. This record is
// NOT an ERTS tagged header word and must never be charged as one in public APIs.
// HamtLeafCons is a custody tag; an OTP cons itself has no boxed header word.
internal readonly record struct OwnedMapHeader517(
    OwnedMapHeaderKind517 Kind,
    int PayloadArity,
    int PairCount,
    ushort Bitmap
);
