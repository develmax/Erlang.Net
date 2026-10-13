namespace Erlang;

internal sealed record SystemProfileOptions(string Timestamp, bool HasEvents)
{
    public static SystemProfileOptions Read(Term value)
    {
        string timestamp = SystemProfileNames.Timestamp;
        bool events = false;
        foreach (Term item in Cons.Items(value))
        {
            if (item is not Atom atom)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            switch (atom.Name)
            {
                case SystemProfileNames.Timestamp:
                case SystemProfileNames.MonotonicTimestamp:
                case SystemProfileNames.StrictMonotonicTimestamp:
                    timestamp = atom.Name;
                    break;
                case SystemProfileNames.Scheduler:
                case SystemProfileNames.RunnableProcesses:
                case SystemProfileNames.RunnablePorts:
                case SystemProfileNames.Exclusive:
                    events = true;
                    break;
                default:
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }

        return new(timestamp, events);
    }
}
