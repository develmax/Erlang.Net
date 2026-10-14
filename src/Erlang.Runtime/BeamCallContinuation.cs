namespace Erlang;

internal sealed record BeamCallContinuation(BeamLinkedFunction Function, int InstructionIndex);
