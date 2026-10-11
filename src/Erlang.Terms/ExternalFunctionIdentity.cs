namespace Erlang;

public sealed record ExternalFunctionIdentity(Atom Module, Atom Function, int Arity);
