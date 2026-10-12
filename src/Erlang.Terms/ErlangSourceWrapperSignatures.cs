namespace Erlang;

public static class ErlangSourceWrapperSignatures
{
    public static bool Contains(string function, int arity) => arity is 2 or 4 && function is ErlangModuleNames.Spawn or ErlangModuleNames.SpawnLink or ErlangModuleNames.SpawnMonitor || function == RuntimeNodeQueryNames.IsAlive && arity == 0 || function == ProcessSuspensionNames.Suspend && arity is 1 or 2 || arity == 1 && function is ModuleCodeLifecycleNames.PurgeModule or ErlangStandardStreamNames.DisplayString;
}
