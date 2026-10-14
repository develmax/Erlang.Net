namespace Erlang.Differential;

public sealed record OracleProgressEvidence(
    string Source,
    OracleAbortEvidence NativeEvidence,
    int CompletedComparisons
);
