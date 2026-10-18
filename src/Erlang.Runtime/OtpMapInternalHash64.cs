namespace Erlang;

internal static class OtpMapInternalHash64
{
    internal static ulong Hash(Term term, OtpMapHashIdentityProvider identities)
    {
        var state = new OtpMapHashState64(identities);

        return state.TryImmediateWord(term, out ulong word)
            ? Mix64(word) : state.HashStructured(term);
    }

    internal static ulong Mix64(ulong value)
    {
        unchecked
        {
            value ^= value >> 33;
            value *= OtpMapHashNames.FinalFirstMultiplier;
            value ^= value >> 33;
            value *= OtpMapHashNames.FinalSecondMultiplier;

            return value ^ (value >> 33);
        }
    }
}
