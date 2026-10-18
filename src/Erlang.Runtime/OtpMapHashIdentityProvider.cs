namespace Erlang;

internal abstract class OtpMapHashIdentityProvider
{
    // The word is an actual modeled 64-bit Eterm, not phash2 or an atom-name hash.
    internal abstract bool TryImmediateWord(Term term, out ulong word);

    // Supplies native identity/layout protocol for map, record, FUN and reference
    // terms and external PID/port terms. An unresolved protocol must fail explicitly.
    internal abstract void AppendStructured(Term term, OtpMapHashState64 state);
}
