namespace Erlang.Otp;

internal static class ErpcTimeout
{
    internal static async ValueTask<Term> Normalize(ProcessContext context, Term value)
    {
        if (value.Equals(ErpcAtoms.Infinity))
            return value;
        if (value is Integer milliseconds && milliseconds.Value >= 0 && milliseconds.Value <= ErpcNames.MaximumTimeout)
            return value;
        if (value is TupleTerm { Items: [Atom { Name: ErpcNames.Abs }, Integer deadline] })
        {
            var current = (Integer)await context.Runtime.Modules.Call(
                context,
                ProcLibNames.Erlang,
                ErpcNames.MonotonicTime,
                Term.A(ErpcNames.Millisecond)
            );
            var remaining = deadline.Value - current.Value;
            if (remaining <= 0)
                return Term.I(0);
            if (remaining <= ErpcNames.MaximumTimeout)
                return new Integer(remaining);
        }

        throw new ErlangException(Term.Tuple(ErpcAtoms.Module, ErpcAtoms.BadArgument));
    }
}
