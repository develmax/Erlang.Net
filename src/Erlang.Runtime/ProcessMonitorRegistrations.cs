namespace Erlang;

internal static class ProcessMonitorRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            ErlangModuleNames.Monitor,
            3,
            (context, arguments) =>
        {
            bool priority = false;
            string? mode = null;
            string kind = CoreModules.AtomName(arguments[0]);
            Term tag = ProcessMonitorOptions.ForKind(kind).Tag;
            foreach (Term option in Cons.Items(arguments[2]))
            {
                if (option is Atom { Name: ProcessMonitorOptionNames.Priority })
                    priority = true;
                else if (option is TupleTerm { Items.Count: 2 } pair && pair.Items[0] is Atom key)
                {
                    if (key.Name == ProcessMonitorOptionNames.Tag)
                        tag = pair.Items[1];
                    else if (key.Name == ProcessMonitorOptionNames.Alias && pair.Items[1] is Atom { Name: ProcessAliasNames.Explicit or ProcessMonitorOptionNames.Demonitor or ProcessMonitorOptionNames.ReplyDemonitor } activation)
                        mode = activation.Name;
                    else
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                }
                else
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            if (kind is not ProcessMonitorKinds.Process and not ProcessMonitorKinds.TimeOffset)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return ValueTask.FromResult<Term>(context.Runtime.MonitorSource(
                context,
                kind,
                arguments[1],
                new(priority, mode, tag)
            ));
        }
        );
        registry.Register(
            ErlangModuleNames.Module,
            ErlangModuleNames.Demonitor,
            2,
            (context, arguments) =>
        {
            bool flush = false, info = false;
            foreach (Term option in Cons.Items(arguments[1]))
            {
                switch (option)
                {
                    case Atom { Name: ProcessMonitorOptionNames.Flush }:
                        flush = true;
                        break;
                    case Atom { Name: ProcessMonitorOptionNames.Info }:
                        info = true;
                        break;
                    default:
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                }
            }
            if (arguments[0] is not ReferenceTerm reference)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return ValueTask.FromResult<Term>(CoreModules.Boolean(context.Runtime.Demonitor(
                context,
                reference,
                flush,
                info
            )));
        }
        );
    }
}
