namespace Erlang;

public sealed class RuntimeLibraryModuleSnapshot
{
    internal RuntimeLibraryModuleSnapshot(
        string module,
        RuntimeModuleBackend backend,
        ModuleCodeImage image,
        string? inputSha256,
        BitString nativeMd5,
        IEnumerable<BeamFunctionSignature> exports,
        IEnumerable<BeamFunctionSignature> nativePrimitives,
        IEnumerable<RuntimeLibraryAssemblyIdentity> managedAssemblies,
        IEnumerable<RuntimeLibraryDependencySnapshot> dependencies
    )
    {
        Module = module;
        Backend = backend;
        Image = image;
        InputSha256 = inputSha256;
        NativeMd5 = nativeMd5;
        Exports = Array.AsReadOnly(exports.OrderBy(value => value.Name, StringComparer.Ordinal).ThenBy(value => value.Arity).ToArray());
        NativePrimitives = Array.AsReadOnly(nativePrimitives.OrderBy(value => value.Name, StringComparer.Ordinal).ThenBy(value => value.Arity).ToArray());
        ManagedAssemblies = Array.AsReadOnly(managedAssemblies.Distinct().OrderBy(value => value.AssemblyName, StringComparer.Ordinal).ThenBy(value => value.ModuleVersionId).ToArray());
        Dependencies = Array.AsReadOnly(dependencies.OrderBy(value => value.Module, StringComparer.Ordinal).ToArray());
    }

    public string Module { get; }
    public RuntimeModuleBackend Backend { get; }
    public ModuleCodeImage Image { get; }
    public string? InputSha256 { get; }
    public BitString NativeMd5 { get; }
    public IReadOnlyList<BeamFunctionSignature> Exports { get; }
    public IReadOnlyList<BeamFunctionSignature> NativePrimitives { get; }
    public IReadOnlyList<RuntimeLibraryAssemblyIdentity> ManagedAssemblies { get; }
    public IReadOnlyList<RuntimeLibraryDependencySnapshot> Dependencies { get; }
}
