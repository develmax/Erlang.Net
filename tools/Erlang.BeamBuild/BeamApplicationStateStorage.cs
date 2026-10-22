using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Erlang.BeamBuild;

internal static class BeamApplicationStateStorage
{
    internal static BeamApplicationState? Read(string output)
    {
        string path = Path.Combine(output, BeamApplicationBuildNames.State);
        RequireDestination(output, BeamApplicationBuildNames.State);
        if (!File.Exists(path))
            return null;
        var state = JsonSerializer.Deserialize<BeamApplicationState>(
            BeamApplicationRead.Bytes(path, BeamApplicationNames.MaximumFileBytes),
            new JsonSerializerOptions { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow }
        )
            ?? throw new InvalidDataException(BeamApplicationNames.Invalid);
        if (state.Schema != BeamApplicationBuildNames.StateSchema || state.Files is null || state.NativeEvidence is null
            || !Regex.IsMatch(state.ApplicationName ?? string.Empty, BeamBuildValidationNames.ModuleIdentifier)
            || !Regex.IsMatch(state.Version ?? string.Empty, BeamApplicationNames.VersionPattern)
            || !Digest(state.RequestSha256) || !Digest(state.ConfigurationSha256)
            || state.Files.Length < 2 || state.Files.Length > BeamApplicationNames.MaximumFiles + 1)
            throw new InvalidDataException(BeamApplicationNames.Invalid);
        string prefix = BeamApplicationNames.LibraryDirectory + "/" + state.ApplicationName + "-" + state.Version + "/";
        var destinations = new BeamApplicationPathSet();
        int manifests = 0;
        int descriptors = 0;
        foreach (BeamApplicationOutput row in state.Files)
        {
            if (row is null || row.TargetPath is null || !row.TargetPath.StartsWith(prefix, StringComparison.Ordinal)
                || !Digest(row.Sha256) || row.Bytes < 0 || row.Bytes > BeamApplicationNames.MaximumFileBytes)
                throw new InvalidDataException(BeamApplicationNames.Invalid);
            destinations.AddFile(row.TargetPath);
            string expected = RequireDestination(output, row.TargetPath);
            if (!string.Equals(row.Path, expected, StringComparison.Ordinal))
                throw new InvalidDataException(BeamApplicationNames.Invalid);
            string relative = row.TargetPath[prefix.Length..];
            bool valid = row.Kind switch
            {
                BeamApplicationBuildNames.ManifestKind => relative == BeamApplicationNames.ManifestFile && ++manifests == 1,
                BeamApplicationBuildNames.DescriptorKind => relative == BeamApplicationNames.EbinDirectory + "/" + state.ApplicationName + BeamApplicationNames.AppExtension && ++descriptors == 1,
                BeamApplicationBuildNames.BeamKind => relative.StartsWith(BeamApplicationNames.EbinDirectory + "/", StringComparison.Ordinal) && relative.EndsWith(BeamBuildNames.BeamExtension, StringComparison.Ordinal),
                BeamApplicationBuildNames.PrivKind => relative.StartsWith(BeamApplicationNames.PrivDirectory + "/", StringComparison.Ordinal),
                BeamApplicationBuildNames.ConfigurationKind => relative.StartsWith(BeamApplicationNames.ConfigurationDirectory + "/", StringComparison.Ordinal),
                _ => false
            };
            if (!valid || File.Exists(expected) && (new FileInfo(expected).Length != row.Bytes || BeamBuildStorage.HashFile(expected) != row.Sha256))
                throw new InvalidDataException(BeamApplicationNames.Invalid);
        }
        BeamApplicationNativeEvidence evidence = state.NativeEvidence;
        if (manifests != 1 || descriptors != 1 || !Digest(evidence.AdmissionReceiptSha256)
            || !Digest(evidence.CompilerSha256) || !Digest(evidence.RuntimeSha256)
            || !Digest(evidence.DriverSourceSha256) || !Digest(evidence.DriverBeamSha256)
            || !Digest(evidence.ParsedEtfSha256) || !Digest(evidence.ToolAssemblySha256)
            || string.IsNullOrWhiteSpace(evidence.ParsedEtfPath) || !Path.IsPathFullyQualified(evidence.ParsedEtfPath)
            || !BeamApplicationPaths.Within(output, evidence.ParsedEtfPath))
            throw new InvalidDataException(BeamApplicationNames.Invalid);
        BeamApplicationPaths.RequireActualCase(evidence.ParsedEtfPath);
        if (BeamBuildStorage.Hash(BeamApplicationRead.Bytes(evidence.ParsedEtfPath, BeamApplicationNames.MaximumFileBytes)) != evidence.ParsedEtfSha256)
            throw new InvalidDataException(BeamApplicationNames.Invalid);

        return state;
    }

    internal static string RequireDestination(string output, string logical)
    {
        BeamApplicationPaths.RequireLogical(logical);
        string path = Path.GetFullPath(Path.Combine(output, logical));
        if (!BeamApplicationPaths.Within(output, path) || BeamDeclaredPaths.Relative(output, path) != logical)
            throw new InvalidDataException(BeamApplicationNames.Invalid);
        BeamApplicationPaths.RequireCreationPath(Path.GetDirectoryName(path)!);
        BeamDeclaredPaths.RequireNoReparse(path);
        if (Directory.Exists(path))
            throw new InvalidDataException(BeamApplicationNames.Invalid);
        if (File.Exists(path))
            BeamApplicationPaths.RequireActualCase(path);

        return path;
    }

    internal static string Kind(string input) => input switch
    {
        BeamApplicationNames.ModuleKind => BeamApplicationBuildNames.BeamKind,
        BeamApplicationNames.DescriptorKind => BeamApplicationBuildNames.DescriptorKind,
        BeamApplicationNames.PrivKind => BeamApplicationBuildNames.PrivKind,
        BeamApplicationNames.ConfigurationKind => BeamApplicationBuildNames.ConfigurationKind,
        _ => throw new InvalidDataException(BeamApplicationNames.Invalid)
    };

    private static bool Digest(string? value) => Regex.IsMatch(value ?? string.Empty, BeamBuildValidationNames.Sha256);
}
