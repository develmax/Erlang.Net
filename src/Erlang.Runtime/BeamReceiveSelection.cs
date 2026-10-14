namespace Erlang;

internal sealed record BeamReceiveSelection(int NextInstructionIndex, BeamRegisterState Registers);
