namespace Erlang;

internal static class PreloadedModuleRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            PreloadedModuleNames.PreLoaded,
            0,
            (context, _) => ValueTask.FromResult(context.Runtime.Modules.PreloadedModuleList())
        );
    }
}
