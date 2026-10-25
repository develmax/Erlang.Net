using System.Numerics;

namespace Erlang;

internal readonly record struct ClrMemorySnapshot(
    BigInteger Processes,
    BigInteger ProcessesUsed,
    BigInteger Atom,
    BigInteger Binary,
    BigInteger Code,
    BigInteger Native
)
{
    private BigInteger System => Atom + Binary + Code + Native;

    internal Integer Select(Term key) => new(key is global::Erlang.Atom name ? name.Name switch
    {
        ClrMemoryNames.Total => Processes + System,
        ClrMemoryNames.Processes => Processes,
        ClrMemoryNames.ProcessesUsed => ProcessesUsed,
        ClrMemoryNames.System => System,
        ClrMemoryNames.Atom or ClrMemoryNames.AtomUsed => Atom,
        ClrMemoryNames.Binary => Binary,
        ClrMemoryNames.Code => Code,
        // The SDK has no ETS storage subsystem. Declaration metadata is not a table.
        ClrMemoryNames.Ets => BigInteger.Zero,
        _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
    } : throw new ErlangException(ErlangErrorReasons.BadArgument));

    internal Term All()
    {
        var pairs = new Term[ClrMemoryNames.Ordered.Length];
        for (int index = 0; index < pairs.Length; index++)
        {
            var key = Term.A(ClrMemoryNames.Ordered[index]);
            pairs[index] = Term.Tuple(key, Select(key));
        }

        return Term.List(pairs);
    }
}
