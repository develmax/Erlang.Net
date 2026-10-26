namespace Erlang;

internal static class ProcessIdentifierTextRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            ProcessIdentifierTextNames.PidToList,
            1,
            (context, arguments) => arguments[0] is Pid pid ? ValueTask.FromResult(ProcessIdentifierText.PidList(context.Runtime, pid)) : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        registry.Register(
            ErlangModuleNames.Module,
            ProcessIdentifierTextNames.ListToPid,
            1,
            (context, arguments) => ValueTask.FromResult<Term>(ProcessIdentifierText.Pid(context.Runtime, arguments[0]))
        );
        registry.Register(
            ErlangModuleNames.Module,
            ProcessIdentifierTextNames.PortToList,
            1,
            (context, arguments) => arguments[0] is PortTerm port ? ValueTask.FromResult(ProcessIdentifierText.PortList(context.Runtime, port)) : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        registry.Register(
            ErlangModuleNames.Module,
            ProcessIdentifierTextNames.ListToPort,
            1,
            (context, arguments) => ValueTask.FromResult<Term>(ProcessIdentifierText.Port(context.Runtime, arguments[0]))
        );
    }
}
