namespace Erlang;

internal sealed record BeamOpcodeDefinition(
    byte Opcode,
    string Name,
    int Arity,
    bool Obsolete
);
