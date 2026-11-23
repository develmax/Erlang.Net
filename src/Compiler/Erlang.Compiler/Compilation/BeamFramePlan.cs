namespace Erlang.Compiler;

// Origins use the order in which OTP reserves the independent Y pools.
// Owners contains reachable ordinary blocks. A null owner is frameless.
internal sealed record BeamFramePlan(
    IReadOnlyList<int> Origins,
    IReadOnlyDictionary<int, int?> Owners
);
