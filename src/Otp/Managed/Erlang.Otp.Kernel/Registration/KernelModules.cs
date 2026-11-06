namespace Erlang.Otp;

public static class KernelModules
{
    public static void Register(ModuleRegistry registry)
        => registry.InstallSourceWrappers(code =>
        {
            RegisterReportDependencies(code);
            RegisterRpc(code);
        });

    internal static void RegisterReportDependencies(ModuleRegistry registry)
        => ProcLibCrashReportDependencies.Register(registry);

    internal static void RegisterRpc(ModuleRegistry registry)
    {
        if (!registry.HasExport(ErpcNames.Module, ErpcNames.Call, 5))
        {
            Erpc.RegisterLegacy(registry);
        }
        Rpc.Register(registry);
    }
}
