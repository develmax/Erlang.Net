namespace Erlang;

internal static class ClrMemoryRegistrations
{
    internal static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            ClrMemoryNames.Function,
            ClrMemoryNames.AllArity,
            (context, _) => ClrMemoryQuery.Run(context, null)
        );
        registry.Register(
            ErlangModuleNames.Module,
            ClrMemoryNames.Function,
            ClrMemoryNames.QueryArity,
            (context, arguments) => ClrMemoryQuery.Run(context, arguments[0])
        );
    }
}
