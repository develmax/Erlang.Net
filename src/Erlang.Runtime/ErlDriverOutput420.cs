namespace Erlang;

// Header always constructs list cells before the mode-dependent body. Keeping
// it separate is essential for binary-mode improper-list output2 semantics.
internal sealed record ErlDriverOutput420(
    ErlDriverOutputKind420 Kind,
    byte[] Header,
    byte[] Body,
    byte[][]? Segments = null,
    bool BodyPresent = true
);
