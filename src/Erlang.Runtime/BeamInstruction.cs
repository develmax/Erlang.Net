namespace Erlang;

internal sealed record BeamInstruction(int Offset, BeamOpcodeDefinition Definition, IReadOnlyList<BeamOperand> Operands);
