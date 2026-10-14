namespace Erlang;

internal sealed record BeamLinkedFunction(
    string Module,
    string Name,
    int Arity,
    int InfoLabel,
    int EntryLabel,
    int FirstInstructionIndex,
    int EndInstructionIndex
);
