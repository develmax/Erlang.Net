using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;

namespace Erlang.BeamBuild;

internal sealed record BeamBuildRequest(
    string InstalledRoot,
    string ProvenanceKind,
    string AdmissionReceipt,
    string AdmissionReceiptSha256,
    string CompilerSha256,
    string OutputDirectory,
    BeamBuildSource[] Sources,
    string? SourcePolicy = null,
    string? SourceRoot = null,
    BeamBuildHeader[]? Headers = null,
    string[]? IncludeDirectories = null,
    BeamBuildDefine[]? Defines = null,
    string? RuntimeSha256 = null
)
{
    internal static BeamBuildRequest Read(string requestPath)
    {
        if (!Path.IsPathFullyQualified(requestPath))
            throw new InvalidDataException(BeamBuildNames.Invalid);
        BeamBuildRequest value = JsonSerializer.Deserialize<BeamBuildRequest>(File.ReadAllBytes(requestPath), new JsonSerializerOptions { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow })
            ?? throw new InvalidDataException(BeamBuildNames.Invalid);

        return Validate(value);
    }

    internal static BeamBuildRequest Validate(BeamBuildRequest value)
    {
        foreach (string path in new[] { value.InstalledRoot, value.AdmissionReceipt, value.OutputDirectory })
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
                throw new InvalidDataException(BeamBuildNames.Invalid);
        if (value.ProvenanceKind is not (BeamBuildProvenanceNames.SourceBuild or BeamBuildProvenanceNames.OfficialRelease)
            || !Regex.IsMatch(value.AdmissionReceiptSha256 ?? string.Empty, BeamBuildValidationNames.Sha256)
            || !Regex.IsMatch(value.CompilerSha256 ?? string.Empty, BeamBuildValidationNames.Sha256) || value.Sources is not { Length: > 0 })
            throw new InvalidDataException(BeamBuildNames.Invalid);
        var modules = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (BeamBuildSource source in value.Sources)
            if (source is null || source.Module is null || !Regex.IsMatch(source.Module, BeamBuildValidationNames.ModuleIdentifier)
                || !Path.IsPathFullyQualified(source.Path) || !File.Exists(source.Path)
                || !modules.Add(source.Module))
                throw new InvalidDataException(BeamBuildNames.Invalid);

        string installed = Path.TrimEndingDirectorySeparator(Path.GetFullPath(value.InstalledRoot)) + Path.DirectorySeparatorChar;
        string output = Path.GetFullPath(value.OutputDirectory);
        if (output.StartsWith(installed, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
            || Path.TrimEndingDirectorySeparator(output).Equals(
                Path.TrimEndingDirectorySeparator(installed),
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal
            ))
            throw new InvalidDataException(BeamBuildNames.Invalid);
        for (string? ancestor = output; ancestor is not null; ancestor = Path.GetDirectoryName(ancestor))
            if (Directory.Exists(ancestor) && (File.GetAttributes(ancestor) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException(BeamBuildNames.Invalid);

        return BeamDeclaredRequest.Validate(value with { Sources = value.Sources.ToArray() });
    }
}
