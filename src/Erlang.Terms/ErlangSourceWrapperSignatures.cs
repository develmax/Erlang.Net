namespace Erlang;

public static class ErlangSourceWrapperSignatures
{
    public static bool Contains(string function, int arity) => function switch
    {
        PortNames.Open or PortNames.Connect or PortNames.Control when arity is 2 or 3 => true,
        PortNames.Close when arity == 1 => true,
        PortNames.Info when arity is 1 or 2 => true,
        PortNames.Call or PortNames.Command when arity is 2 or 3 => true,
        ErlangModuleNames.ProcessFlag when arity == 3 => true,
        RuntimeNodeControlNames.Disconnect when arity == 1 => true,
        RuntimeNodeControlNames.MonitorTrap when arity == 3 => true,
        ErlangCookieNames.Get when arity is 0 or 1 => true,
        ErlangCookieNames.Set when arity is 1 or 2 => true,
        ErlangModuleNames.Spawn or ErlangModuleNames.SpawnLink or ErlangModuleNames.SpawnMonitor when arity is 2 or 4 => true,
        RuntimeNodeQueryNames.IsAlive when arity == 0 => true,
        ProcessSuspensionNames.Suspend when arity is 1 or 2 => true,
        ModuleCodeLifecycleNames.PurgeModule or ErlangStandardStreamNames.DisplayString when arity == 1 => true,
        _ => false
    };
}
