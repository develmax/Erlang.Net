namespace Erlang;

internal sealed record BeamHandlerCheckpoint(
    BeamLinkedFunction Function,
    int HandlerInstructionIndex,
    int ContinuationDepth,
    int CallFrameDepth,
    int FunctionScopeDepth,
    int RegisterFrameDepth
)
{
    internal bool IsCatch { get; init; }
}
