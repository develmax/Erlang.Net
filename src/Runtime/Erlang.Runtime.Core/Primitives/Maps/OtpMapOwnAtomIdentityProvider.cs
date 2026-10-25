namespace Erlang;

internal sealed class OtpMapOwnAtomIdentityProvider(OtpMapHashIdentityProvider? remaining = null) : OtpMapHashIdentityProvider
{
    internal override bool TryImmediateWord(Term term, out ulong word)
    {
        if (term is Atom atom)
        {
            word = ((ulong)AtomNameRegistry.IndexOf(atom.Name) << OtpMapHashNames.AtomShift) | OtpMapHashNames.AtomTag;

            return true;
        }
        if (remaining is not null)
            return remaining.TryImmediateWord(term, out word);
        word = 0;

        return false;
    }

    internal override void AppendStructured(Term term, OtpMapHashState64 state)
    {
        if (remaining is null)
            throw new ErlangException(Term.A(OtpMapHashNames.BadArgument));
        remaining.AppendStructured(term, state);
    }
}
