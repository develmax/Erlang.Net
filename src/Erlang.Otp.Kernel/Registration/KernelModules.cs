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
        Erpc.Register(registry);
        Rpc.Register(registry);
    }
}
