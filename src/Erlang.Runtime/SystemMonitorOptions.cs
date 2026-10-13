namespace Erlang;

internal sealed record SystemMonitorOptions(long? Disable, ulong Enable)
{
    public static SystemMonitorOptions Parse(Term options)
    {
        long? disable = null;
        ulong enable = 0;
        var physical = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (Term option in Cons.Items(options))
        {
            string name;
            Term value;
            if (option is Atom atom)
            {
                name = atom.Name;
                value = Term.A(ErlangBooleanAtoms.True);
            }
            else if (option is TupleTerm { Items: [Atom key, var item] })
            {
                name = key.Name;
                value = item;
            }
            else
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            switch (name)
            {
                case SystemMonitorNames.LongQueue:
                    if (value is Atom { Name: ErlangBooleanAtoms.False })
                    {
                        disable = null;
                        enable = 0;
                    }
                    else if (value is TupleTerm { Items: [Integer off, Integer on] } && off.Value >= 0 && off.Value <= long.MaxValue && on.Value > off.Value && on.Value <= ulong.MaxValue)
                    {
                        disable = (long)off.Value;
                        enable = (ulong)on.Value;
                    }
                    else
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    break;
                case SystemMonitorNames.LongGarbageCollection:
                case SystemMonitorNames.LongSchedule:
                case SystemMonitorNames.LargeHeap:
                    if (value is Atom { Name: ErlangBooleanAtoms.False })
                        physical[name] = false;
                    else if (value is Integer number && number.Value >= 0 && number.Value <= ulong.MaxValue)
                        physical[name] = true;
                    else
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    break;
                case SystemMonitorNames.BusyPort:
                case SystemMonitorNames.BusyDistributionPort:
                    if (value is not Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False } boolean)
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    physical[name] = boolean.Name == ErlangBooleanAtoms.True;
                    break;
                default:
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }
        foreach (string name in new[] { SystemMonitorNames.BusyDistributionPort, SystemMonitorNames.BusyPort, SystemMonitorNames.LargeHeap, SystemMonitorNames.LongSchedule, SystemMonitorNames.LongGarbageCollection })
        {
            if (physical.GetValueOrDefault(name))
                throw SystemMonitorNames.UnsupportedEffect(name);
        }

        return new(disable, enable);
    }

    public Term ToTerm() => Disable is long off ? Term.List(Term.Tuple(Term.A(SystemMonitorNames.LongQueue), Term.Tuple(Term.I(off), new Integer(Enable)))) : Nil.Value;
}
