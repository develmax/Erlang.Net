namespace Erlang;

internal sealed record BeamReceivePlan(
    int ScanInstructionIndex,
    int MessageRegister,
    int WaitInstructionIndex,
    int TimeoutInstructionIndex,
    TimeSpan? Timeout,
    IReadOnlySet<int> MatcherInstructions
);
