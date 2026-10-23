namespace Erlang;

internal sealed record BeamReceiveSelection(int NextInstructionIndex, BeamRegisterState Registers, IReadOnlyList<Term> ClearMarkers);
