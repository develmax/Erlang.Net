using System.Reflection;
using System.Runtime.Loader;

namespace Erlang;

internal static class RuntimeManagedLibraries456
{
    // The composition project ships this fixed dependency. Resolve it in the
    // runtime's own load context, without directory discovery or backend fallback.
    private static readonly Lazy<IRuntimeManagedLibrary456> Stdlib = new(() =>
    {
        Assembly runtime = typeof(ModuleRegistry).Assembly;
        AssemblyLoadContext context = AssemblyLoadContext.GetLoadContext(runtime)!;
        Assembly assembly = context.LoadFromAssemblyName(new AssemblyName(RuntimeManagedLibraryNames456.StdlibAssembly));
        Type entry = assembly.GetType(RuntimeManagedLibraryNames456.StdlibEntry, throwOnError: true)!;

        return Activator.CreateInstance(entry) as IRuntimeManagedLibrary456
            ?? throw new InvalidOperationException(RuntimeManagedLibraryNames456.InvalidEntry);
    });

    private static readonly Lazy<IRuntimeManagedLibrary456> Kernel = new(() =>
    {
        AssemblyLoadContext context = AssemblyLoadContext.GetLoadContext(typeof(ModuleRegistry).Assembly)!;
        Assembly assembly = context.LoadFromAssemblyName(new AssemblyName(RuntimeManagedLibraryNames456.KernelAssembly));
        Type entry = assembly.GetType(RuntimeManagedLibraryNames456.KernelEntry, throwOnError: true)!;

        return Activator.CreateInstance(entry) as IRuntimeManagedLibrary456
            ?? throw new InvalidOperationException(RuntimeManagedLibraryNames456.InvalidEntry);
    });

    internal static void Register(ModuleRegistry registry, RuntimeManagedLibraryStage456 stage)
        => (stage is RuntimeManagedLibraryStage456.Erpc or RuntimeManagedLibraryStage456.ErrorLogger or RuntimeManagedLibraryStage456.Logger or RuntimeManagedLibraryStage456.File or RuntimeManagedLibraryStage456.Application ? Kernel.Value : Stdlib.Value).Register(registry, stage);

    internal static Assembly StdlibAssembly => Stdlib.Value.GetType().Assembly;
}
