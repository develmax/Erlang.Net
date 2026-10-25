namespace Erlang;

internal sealed record BeamExternalImport(string Module, string Function, int Arity);
