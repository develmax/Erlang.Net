namespace Erlang;

internal sealed record ErlDriverLoadOptions420(
    bool KillPorts,
    ErlDriverWait420 Monitor,
    ErlDriverWait420 Reload
)
{
    // Source ordering: complete options validation, then name, then native path.
    // Duplicate monitor/reload are last-wins; driver flags accumulate by OR.
    internal static ErlDriverLoadOptions420 Parse(Term value, CancellationToken cancellation)
    {
        bool killPorts = false;
        ErlDriverWait420 monitor = ErlDriverWait420.None;
        ErlDriverWait420 reload = ErlDriverWait420.None;
        while (value is Cons cell)
        {
            cancellation.ThrowIfCancellationRequested();
            if (cell.Head is not TupleTerm { Items.Count: 2 } option || option.Items[0] is not Atom key)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            switch (key.Name)
            {
                case ErlDriverNames420.DriverOptions:
                    Term flags = option.Items[1];
                    while (flags is Cons flag)
                    {
                        cancellation.ThrowIfCancellationRequested();
                        if (flag.Head is not Atom { Name: ErlDriverNames420.KillPorts })
                            throw new ErlangException(ErlangErrorReasons.BadArgument);
                        killPorts = true;
                        flags = flag.Tail;
                    }
                    if (flags is not Nil) throw new ErlangException(ErlangErrorReasons.BadArgument);
                    break;
                case ErlDriverNames420.Monitor:
                    monitor = ReadWait(option.Items[1]);
                    break;
                case ErlDriverNames420.Reload:
                    reload = ReadWait(option.Items[1]);
                    break;
                default:
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            value = cell.Tail;
        }
        if (value is not Nil) throw new ErlangException(ErlangErrorReasons.BadArgument);

        return new(killPorts, monitor, reload);
    }

    internal static ErlDriverWait420 ReadWait(Term value) => value switch
    {
        Atom { Name: ErlDriverNames420.PendingDriver } => ErlDriverWait420.PendingDriver,
        Atom { Name: ErlDriverNames420.Pending } => ErlDriverWait420.Pending,
        _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
    };
}
