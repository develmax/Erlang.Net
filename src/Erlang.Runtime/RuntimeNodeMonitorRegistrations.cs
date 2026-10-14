namespace Erlang;

internal static class RuntimeNodeMonitorRegistrations
{
    internal static bool TryChangeSelf(ProcessContext context, IReadOnlyList<Term> arguments, out Term result)
    {
        Term options;
        if (arguments[0] is Atom { Name: RuntimeNodeMonitorNames.Flag })
            options = Nil.Value;
        else if (arguments[0] is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0] is Atom { Name: RuntimeNodeMonitorNames.Flag })
            options = tuple.Items[1];
        else
        {
            result = Nil.Value;

            return false;
        }
        bool enable = RuntimeNodeMonitorOptions.Boolean(arguments[1]);
        RuntimeNodeMonitorOptions parsed = RuntimeNodeMonitorOptions.Parse(options);
        try
        {
            result = new Integer(context.Runtime.LocalNodeEvents.Monitor(context, enable, parsed));
        }
        catch (ArgumentException)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }

        return true;
    }
}
