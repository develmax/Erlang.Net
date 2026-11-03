namespace Erlang;

internal sealed record StringBinaryMatch(
    bool Matched,
    BitString Before,
    Term At,
    Term After,
    int ConsumedBytes,
    Term Continuation
);
