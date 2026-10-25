namespace Erlang;

internal sealed record BeamExecutionIdentity(ModuleCodeImage Image, object Owner)
{
    internal IReadOnlyDictionary<(string Function, int Arity), ErlangFunction> NativePrimitives { get; init; }
        = System.Collections.Frozen.FrozenDictionary<(string Function, int Arity), ErlangFunction>.Empty;
}
