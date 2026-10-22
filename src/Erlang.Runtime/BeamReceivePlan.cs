namespace Erlang;

internal sealed record BeamReceivePlan(
    int ScanInstructionIndex,
    int MessageRegister,
    int WaitInstructionIndex,
    int TimeoutInstructionIndex,
    BeamOperand? TimeoutOperand,
    IReadOnlySet<int> MatcherInstructions
)
{
    internal BeamReceiveCallbackPlan? Callback { get; init; }

    internal bool HasTimeout => TimeoutInstructionIndex != BeamReceiveNames.NoTimeoutInstruction;
    internal bool HasMatcher => MessageRegister != BeamReceiveNames.NoMessageRegister;
}
