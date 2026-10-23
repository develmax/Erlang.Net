namespace Erlang;

internal sealed record BeamCallFrame(
    int FunctionEntryLabel,
    int ContinuationDepth,
    BeamCallContinuation? SavedContinuation,
    int Slots
);
