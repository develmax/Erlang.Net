namespace Erlang;

internal sealed record BeamZeroFrame(int FunctionEntryLabel, int ContinuationDepth, BeamCallContinuation? SavedContinuation);
