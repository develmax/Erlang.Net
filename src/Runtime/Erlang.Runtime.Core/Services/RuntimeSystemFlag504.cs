namespace Erlang;

internal static class RuntimeSystemFlag504
{
    internal static bool TrySet(Term selector, out Term result)
    {
        // The release OTP bif.c branch deliberately ignores the second argument.
        if (selector is Atom { Name: RuntimeSystemFlag504Names.DebugFlags })
        {
            result = Term.A(ErlangBooleanAtoms.True);

            return true;
        }

        result = Nil.Value;

        return false;
    }
}
