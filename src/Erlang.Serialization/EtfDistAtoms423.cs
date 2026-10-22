namespace Erlang;

internal static class EtfDistAtoms423
{
    internal static bool IsAtomTag(byte tag) => tag is EtfTags.AtomCacheReference or EtfTags.InternalAtomReference2 or EtfTags.InternalAtomReference3
        or EtfTags.Latin1Atom or EtfTags.Utf8Atom or EtfTags.SmallLatin1Atom or EtfTags.SmallUtf8Atom;

    internal static Atom ReadInternal(bool enabled, int index)
    {
        if (!enabled || !AtomNameRegistry.TryNameAt423(index, out string name))
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return Term.A(name);
    }
}
