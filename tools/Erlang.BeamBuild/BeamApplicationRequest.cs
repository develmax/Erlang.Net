using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Erlang.BeamBuild;

internal sealed record BeamApplicationRequest(
    string InstalledRoot,
    string ProvenanceKind,
    string AdmissionReceipt,
    string AdmissionReceiptSha256,
    string CompilerSha256,
    string RuntimeSha256,
    string SourceRoot,
    string OutputDirectory,
    string ApplicationName,
    string Version,
    string Descriptor,
    BeamApplicationModule[] Modules,
    BeamApplicationFile[] Priv,
    BeamApplicationFile[] Configuration,
    string[] ReservedOutputPaths
)
{
    [JsonIgnore]
    internal string RequestSha256 { get; init; } = string.Empty;

    internal string Layout => BeamApplicationNames.LibraryDirectory + BeamApplicationNames.LogicalSeparator + ApplicationName + BeamApplicationNames.VersionSeparator + Version;

    internal static BeamApplicationRequest Read(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
            throw new InvalidDataException(BeamApplicationNames.Invalid);
        byte[] bytes = BeamApplicationRead.Bytes(path, BeamApplicationNames.MaximumRequestBytes);
        var request = JsonSerializer.Deserialize<BeamApplicationRequest>(bytes, new JsonSerializerOptions
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        }) ?? throw new InvalidDataException(BeamApplicationNames.Invalid);

        return Validate(request) with { RequestSha256 = BeamBuildStorage.Hash(bytes) };
    }

    internal static BeamApplicationRequest Validate(BeamApplicationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        foreach (string path in new[] { request.InstalledRoot, request.AdmissionReceipt, request.SourceRoot, request.OutputDirectory })
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
                throw new InvalidDataException(BeamApplicationNames.Invalid);
        if (request.ProvenanceKind is not (BeamBuildProvenanceNames.SourceBuild or BeamBuildProvenanceNames.OfficialRelease)
            || !Digest(request.AdmissionReceiptSha256) || !Digest(request.CompilerSha256) || !Digest(request.RuntimeSha256)
            || request.ApplicationName is null || !Regex.IsMatch(request.ApplicationName, BeamBuildValidationNames.ModuleIdentifier)
            || request.Version is null || !Regex.IsMatch(request.Version, BeamApplicationNames.VersionPattern)
            || request.Version.EndsWith('.') || request.Modules is null || request.Priv is null
            || request.Configuration is null || request.ReservedOutputPaths is null
            || 1L + request.Modules.Length + request.Priv.Length + request.Configuration.Length > BeamApplicationNames.MaximumFiles)
            throw new InvalidDataException(BeamApplicationNames.Invalid);
        string installed = Path.GetFullPath(request.InstalledRoot);
        string sourceRoot = Path.GetFullPath(request.SourceRoot);
        string output = Path.GetFullPath(request.OutputDirectory);
        if (!Directory.Exists(installed) || !Directory.Exists(sourceRoot) || !File.Exists(request.AdmissionReceipt)
            || File.Exists(output) || BeamApplicationPaths.Within(installed, output) || BeamApplicationPaths.Within(output, installed)
            || BeamApplicationPaths.Within(sourceRoot, output) || BeamApplicationPaths.Within(output, sourceRoot))
            throw new InvalidDataException(BeamApplicationNames.Invalid);
        BeamApplicationPaths.RequireActualCase(installed);
        BeamApplicationPaths.RequireActualCase(sourceRoot);
        BeamApplicationPaths.RequireActualCase(request.AdmissionReceipt);
        BeamApplicationPaths.RequireCreationPath(output);
        if (Directory.Exists(output))
            BeamApplicationPaths.RequireActualCase(output);
        string descriptor = BeamApplicationPaths.ContainedFile(sourceRoot, request.Descriptor);
        string filename = Path.GetFileName(descriptor);
        if (filename != request.ApplicationName + BeamApplicationNames.AppExtension
            && filename != request.ApplicationName + BeamApplicationNames.AppSourceExtension)
            throw new InvalidDataException(BeamApplicationNames.Invalid);
        var destinations = new BeamApplicationPathSet();
        foreach (string reserved in request.ReservedOutputPaths)
            destinations.AddFile(reserved);
        destinations.AddFile(request.Layout + BeamApplicationNames.LogicalSeparator + BeamApplicationNames.ManifestFile);
        destinations.AddFile(request.Layout + BeamApplicationNames.LogicalSeparator + BeamApplicationNames.EbinDirectory + BeamApplicationNames.LogicalSeparator + request.ApplicationName + BeamApplicationNames.AppExtension);
        var inputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { descriptor };
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (BeamApplicationModule module in request.Modules)
        {
            if (module is null || module.Module is null || !Regex.IsMatch(module.Module, BeamBuildValidationNames.ModuleIdentifier)
                || !Digest(module.Sha256) || !names.Add(module.Module)
                || string.IsNullOrWhiteSpace(module.Path) || !Path.IsPathFullyQualified(module.Path)
                || !File.Exists(module.Path) || Path.GetExtension(module.Path) != BeamBuildNames.BeamExtension)
                throw new InvalidDataException(BeamApplicationNames.Invalid);
            string full = Path.GetFullPath(module.Path);
            BeamApplicationPaths.RequireActualCase(full);
            if (BeamApplicationPaths.Within(output, full) || !inputs.Add(full))
                throw new InvalidDataException(BeamApplicationNames.Invalid);
            destinations.AddFile(request.Layout + BeamApplicationNames.LogicalSeparator + BeamApplicationNames.EbinDirectory + BeamApplicationNames.LogicalSeparator + module.Module + BeamBuildNames.BeamExtension);
        }
        foreach (var group in new[] { (request.Priv, BeamApplicationNames.PrivDirectory), (request.Configuration, BeamApplicationNames.ConfigurationDirectory) })
            foreach (BeamApplicationFile file in group.Item1)
            {
                if (file is null || !inputs.Add(BeamApplicationPaths.ContainedFile(sourceRoot, file.Source)))
                    throw new InvalidDataException(BeamApplicationNames.Invalid);
                BeamApplicationPaths.RequireLogical(file.LogicalPath);
                destinations.AddFile(request.Layout + BeamApplicationNames.LogicalSeparator + group.Item2 + BeamApplicationNames.LogicalSeparator + file.LogicalPath);
            }

        return request with
        {
            InstalledRoot = installed,
            AdmissionReceipt = Path.GetFullPath(request.AdmissionReceipt),
            SourceRoot = sourceRoot,
            OutputDirectory = output,
            Modules = request.Modules.Select(module => module with { Path = Path.GetFullPath(module.Path), Sha256 = module.Sha256.ToUpperInvariant() }).ToArray(),
            Priv = request.Priv.ToArray(),
            Configuration = request.Configuration.ToArray(),
            ReservedOutputPaths = request.ReservedOutputPaths.ToArray()
        };
    }

    private static bool Digest(string? value) => Regex.IsMatch(value ?? string.Empty, BeamBuildValidationNames.Sha256);
}
