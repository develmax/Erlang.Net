namespace Erlang;

/// <summary>Canonical VM hash primitives for host library implementations.</summary>
public static class ErlangPortableHash
{
    public static uint Phash2(Term value) => ErlangNativeHash.Portable2(value) & ErlangHashRanges.DefaultMask;

    public static uint Phash(Term value, uint range)
    {
        if (range == 0)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }

        return 1 + ErlangLegacyHash.Portable(value) % range;
    }
}
