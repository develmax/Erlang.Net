namespace Erlang;

internal static class ProcessSchedulerValues
{
    internal static int Parse(Term value, int configured)
    {
        if (value is not Integer integer || integer.Value < ProcessSchedulerNames.Unbound || integer.Value > configured)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return (int)integer.Value;
    }
}
