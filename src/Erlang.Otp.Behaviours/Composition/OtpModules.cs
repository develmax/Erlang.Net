using System.Runtime.CompilerServices;

namespace Erlang.Otp;

public static class OtpModules
{
    private static readonly ConditionalWeakTable<ModuleRegistry, object> Installed = new();

    public static void Register(ModuleRegistry registry)
        => registry.InstallSourceWrappers(RegisterCode);

    private static void RegisterCode(ModuleRegistry registry)
    {
        lock (Installed)
        {
            if (Installed.TryGetValue(registry, out _)) return;
            StdlibProcessModules.RegisterServices(registry);
            KernelModules.RegisterReportDependencies(registry);
            StdlibProcessModules.RegisterProcLib(registry);
            KernelModules.RegisterRpc(registry);
            Installed.Add(registry, new());
        }
    }

    internal static Term WhichChildren(SupervisorHandle handle)
        => StdlibProcessModules.WhichChildren(handle);

    internal static Term CountChildren(SupervisorHandle handle, ProcessRuntime runtime)
        => StdlibProcessModules.CountChildren(handle, runtime);
}
