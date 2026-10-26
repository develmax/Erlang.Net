namespace Erlang;

internal sealed class EtsKeyGroupBuilder420(Term key, ulong hash)
{
    internal Term Key { get; } = key;
    internal ulong Hash { get; } = hash;
    internal List<RuntimeEtsObjectSlot> Rows { get; } = new();
}
