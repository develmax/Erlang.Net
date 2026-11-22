namespace Erlang.Compiler;

// Successor order comes from lowering. To reproduce OTP's RPO it supplies
// failure before success and the default before the ordered switch cases.
internal sealed record BeamFrameBlock(
    int Label,
    IReadOnlyList<int> Successors,
    IReadOnlyList<int> PhiPredecessors,
    bool RequiresFrame,
    bool IsReceiveLoopHeader = false
);
