namespace Erlang;

internal sealed record BeamImport(int ModuleAtomIndex, int FunctionAtomIndex, int Arity);
