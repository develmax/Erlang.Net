namespace Erlang;

internal sealed record ProcessInfoItem(Term Key, string Name, Term? DictionaryKey = null)
{
    public static ProcessInfoItem Parse(Term key)
    {
        if (key is Atom atom && ProcessInfoAtoms.IsSupported(atom))
            return new(key, atom.Name);
        if (key is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0] is Atom { Name: ProcessInfoNames.Dictionary })
            return new(key, ProcessInfoNames.Dictionary, tuple.Items[1]);

        throw new ErlangException(ErlangErrorReasons.BadArgument);
    }

    public bool RequiresOwnerSnapshot => Name is ProcessInfoNames.Links or ProcessInfoNames.Dictionary or ProcessInfoNames.Messages or ProcessInfoNames.MessageQueueLength or
        ProcessInfoNames.Monitors or ProcessInfoNames.MonitoredBy or ProcessInfoNames.CurrentFunction or ProcessInfoNames.CurrentStackTrace or ProcessInfoNames.Status;
}
