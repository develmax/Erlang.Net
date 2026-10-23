namespace Erlang;

internal sealed record BeamClosureBinding(
    BeamLinkedCode Code,
    int TableIndex,
    BeamExecutionIdentity Execution,
    LocalFunctionIdentity Identity
);
