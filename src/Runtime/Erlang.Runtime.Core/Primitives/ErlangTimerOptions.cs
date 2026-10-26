namespace Erlang;

internal sealed record ErlangTimerOptions(bool Absolute, bool Async, bool Info)
{
    public static ErlangTimerOptions Parse(Term value, bool start, bool cancel)
    {
        bool absolute = false;
        bool async = false;
        bool info = true;
        foreach (Term item in Cons.Items(value))
        {
            if (item is not TupleTerm { Items.Count: 2 } tuple || tuple.Items[0] is not Atom option || tuple.Items[1] is not Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False } boolean)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            bool enabled = boolean.Name == ErlangBooleanAtoms.True;
            if (start && option.Name == ErlangTimerOptionNames.Absolute)
                absolute = enabled;
            else if (!start && option.Name == ErlangTimerOptionNames.Async)
                async = enabled;
            else if (!start && cancel && option.Name == ErlangTimerOptionNames.Info)
                info = enabled;
            else
                throw new ErlangException(ErlangErrorReasons.BadArgument);
        }

        return new(absolute, async, info);
    }
}
