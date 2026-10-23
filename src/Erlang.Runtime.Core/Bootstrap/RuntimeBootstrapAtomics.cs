namespace Erlang;

public static class RuntimeBootstrapAtomics
{
    // Constructor is the sole public bootstrap addition. Seven native atomics bodies are captured only for actual original stubs.
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry)) throw new ArgumentException(AtomicsNames413.Foreign, nameof(registry));
        if (IntPtr.Size != sizeof(ulong)) throw new PlatformNotSupportedException(AtomicsNames413.Architecture);
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallBootstrapAtomics413(runtime);
    }
}
