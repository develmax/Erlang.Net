namespace Erlang;

public sealed record RuntimeNativePrimitiveBinding(
    RuntimeNativePrimitives Provider,
    string Module,
    string Function,
    int Arity,
    RuntimeLibraryAssemblyIdentity Assembly
);
