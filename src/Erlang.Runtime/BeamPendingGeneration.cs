namespace Erlang;

internal sealed record BeamPendingGeneration(
    ModuleCodeImage? Current,
    ModuleCodeImage? Old,
    ModuleCodeDescriptor? Descriptor,
    IReadOnlyDictionary<(string Module, string Function, int Arity), ErlangFunction> Bodies
);
