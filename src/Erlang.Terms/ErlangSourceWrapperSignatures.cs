namespace Erlang;

public static class ErlangSourceWrapperSignatures
{
    public static bool Contains(string function, int arity) => function switch
    {
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
