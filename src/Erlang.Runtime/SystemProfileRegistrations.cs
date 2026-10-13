namespace Erlang;

internal static class SystemProfileRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            SystemProfileNames.Function,
            0,
            (context, _) => ValueTask.FromResult(context.Runtime.SystemProfiling.Get())
        );
        registry.Register(
            ErlangModuleNames.Module,
            SystemProfileNames.Function,
            2,
            (context, arguments) => ValueTask.FromResult(context.Runtime.SystemProfiling.Configure(arguments[0], arguments[1]))
        );
    }
}
