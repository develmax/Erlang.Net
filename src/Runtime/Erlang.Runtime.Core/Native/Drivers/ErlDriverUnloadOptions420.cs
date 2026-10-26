namespace Erlang;

internal sealed record ErlDriverUnloadOptions420(bool KillPorts, ErlDriverWait420 Monitor)
{
    internal static ErlDriverUnloadOptions420 Parse(Term value, CancellationToken cancellation)
    {
        bool killPorts = false;
        ErlDriverWait420 monitor = ErlDriverWait420.None;
        while (value is Cons cell)
        {
            cancellation.ThrowIfCancellationRequested();
            if (cell.Head is Atom { Name: ErlDriverNames420.KillPorts }) killPorts = true;
            else if (cell.Head is TupleTerm { Items.Count: 2 } option
                && option.Items[0] is Atom { Name: ErlDriverNames420.Monitor })
                monitor = ErlDriverLoadOptions420.ReadWait(option.Items[1]);
            else throw new ErlangException(ErlangErrorReasons.BadArgument);
            value = cell.Tail;
        }
        if (value is not Nil) throw new ErlangException(ErlangErrorReasons.BadArgument);

        return new(killPorts, monitor);
    }
}
