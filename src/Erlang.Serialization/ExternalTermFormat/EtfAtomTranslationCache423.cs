namespace Erlang;

internal static class EtfAtomTranslationCache423
{
    internal static Atom Read(Atom[]? cache, int index)
    {
        if (cache is null || (uint)index >= (uint)cache.Length)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return cache[index];
    }
}
