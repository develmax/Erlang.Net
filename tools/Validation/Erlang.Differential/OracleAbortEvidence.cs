namespace Erlang.Differential;

public sealed record OracleAbortEvidence(
    string FixtureName,
    string Stage,
    string StandardOutput,
    string StandardError,
    int ExitCode
);
