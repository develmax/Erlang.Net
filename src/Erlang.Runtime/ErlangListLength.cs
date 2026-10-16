namespace Erlang;

internal static class ErlangListLength
{
    // The async state retains the current tail and accumulated length. The
    // caller retains the original argument for the ordinary BIF error frame.
    internal static async ValueTask<Term> ReadAsync(
        ProcessContext context,
        Term original
    )
    {
        Term cursor = original;
        long length = 0;
        while (true)
        {
            await context.ChargeAsync(0);
            int reductions = context.RemainingListTraversalReductions;
            int remaining = reductions * ErlangListLengthLimits.CellsPerReduction;
            int initial = remaining;
            while (cursor is Cons cell && remaining != 0)
            {
                cursor = cell.Tail;
                length++;
                remaining--;
            }
            if (cursor is Cons)
            {
                await context.ChargeAsync(reductions);
                continue;
            }
            if (cursor is not Nil)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            await context.ChargeAsync((initial - remaining) / ErlangListLengthLimits.CellsPerReduction);

            return Term.I(length);
        }
    }

    // OTP's match-spec db_length_1 deliberately does not trap. It must not
    // call the ordinary async BIF through CallMatchSpecGuard.
    internal static Term ReadMatchSpec(Term value)
    {
        long length = 0;
        while (value is Cons cell)
        {
            value = cell.Tail;
            length++;
        }
        if (value is not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return Term.I(length);
    }
}
