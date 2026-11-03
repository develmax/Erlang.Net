using System.Reflection;
using System.Runtime.InteropServices;

namespace Erlang;

// Immutable identity of the actual managed engine. It carries no VM counters,
// provider ERTS version, mutable host state or execution authority.
internal sealed class RuntimeVmBuildDescriptor
{
    internal static readonly RuntimeVmBuildDescriptor Current = new();
    internal string Version { get; }
    internal string SystemArchitecture { get; }

    private RuntimeVmBuildDescriptor()
    {
        Assembly engine = typeof(ProcessRuntime).Assembly;
        string? information = engine.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        string version = string.IsNullOrEmpty(information)
            ? engine.GetName().Version?.ToString() ?? throw new InvalidOperationException(RuntimeVmBuildMetadataNames.MissingVersion)
            : information;
        Guid module = engine.ManifestModule.ModuleVersionId;
        Version = RuntimeVmBuildMetadataNames.EnginePrefix + version
            + RuntimeVmBuildMetadataNames.ModuleSeparator + module.ToString(RuntimeVmBuildMetadataNames.ModuleFormat);
        // RID and process architecture describe the actual CLR ABI hosting all
        // native words of this VM. This is deliberately not an ERTS C triple.
        SystemArchitecture = RuntimeVmBuildMetadataNames.ArchitecturePrefix
            + RuntimeInformation.RuntimeIdentifier + RuntimeVmBuildMetadataNames.ArchitectureSeparator
            + RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
    }
}
