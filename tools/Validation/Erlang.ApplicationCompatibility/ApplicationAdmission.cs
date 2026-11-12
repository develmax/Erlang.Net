using Erlang;
using System.Security.Cryptography;
using System.Text.Json;

internal static class ApplicationAdmission
{
    internal static void Verify(string repository, string directory)
    {
        string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        using JsonDocument provenance = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, ApplicationValidationPaths.Provenance)));
        JsonElement root = provenance.RootElement;
        if (root.GetProperty(ApplicationValidationFields.Baseline).GetString() != ApplicationValidationBaseline.ReleaseTag
            || root.GetProperty(ApplicationValidationFields.Commit).GetString() != ApplicationValidationBaseline.Commit)
        {
            throw new InvalidDataException(ApplicationValidationDiagnostics.BaselineMismatch);
        }
        var checks = new Dictionary<string, string>
        {
            [Path.Combine(directory, ApplicationValidationPaths.Corpus)] = ApplicationValidationFields.CorpusSha256,
            [Path.Combine(directory, ApplicationValidationPaths.Effects)] = ApplicationValidationFields.EffectsSha256,
            [Path.Combine(directory, ApplicationValidationPaths.Exports)] = ApplicationValidationFields.ExportsSha256,
            [Path.Combine(directory, ApplicationValidationPaths.FeatureBootstrap)] = ApplicationValidationFields.FeatureBootstrapSha256,
            [Path.Combine(directory, ApplicationValidationPaths.Prerequisites)] = ApplicationValidationFields.PrerequisitesSha256,
            [Path.Combine(directory, ApplicationValidationPaths.ObserverBeam)] = ApplicationValidationFields.ObserverBeamSha256,
            [Path.Combine(directory, ApplicationValidationPaths.CallbackBeam)] = ApplicationValidationFields.CompanionBeamSha256,
            [Path.Combine(directory, ApplicationValidationPaths.UpstreamBeam)] = ApplicationValidationFields.UpstreamBeamSha256,
            [Path.Combine(repository, ApplicationValidationPaths.ObserverSource)] = ApplicationValidationFields.ObserverSourceSha256,
            [Path.Combine(repository, ApplicationValidationPaths.CallbackSource)] = ApplicationValidationFields.CompanionSourceSha256,
            [Path.Combine(repository, ApplicationValidationPaths.UpstreamSource)] = ApplicationValidationFields.UpstreamSourceSha256
        };
        foreach (var check in checks)
        {
            if (Hash(check.Key) != root.GetProperty(check.Value).GetString())
            {
                throw new InvalidDataException(ApplicationValidationDiagnostics.InputChanged + check.Key);
            }
        }
        foreach (JsonProperty entry in root.GetProperty(ApplicationValidationFields.UpstreamAppFiles).EnumerateObject())
        {
            if (Hash(Path.Combine(repository, ApplicationValidationPaths.UpstreamDirectory, entry.Name)) != entry.Value.GetString())
            {
                throw new InvalidDataException(ApplicationValidationDiagnostics.InputChanged + entry.Name);
            }
        }
        using JsonDocument profile = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repository, ApplicationValidationPaths.OracleAdmission)));
        JsonElement admission = profile.RootElement;
        string manifest = admission.GetProperty(ApplicationValidationFields.ExecutionManifestPath).GetString()!;
        if (Hash(manifest) != root.GetProperty(ApplicationValidationFields.ExecutionManifestSha256).GetString()
            || Hash(manifest) != admission.GetProperty(ApplicationValidationFields.ExecutionManifestSha256).GetString())
        {
            throw new InvalidDataException(ApplicationValidationDiagnostics.ManifestChanged);
        }
        string installed = admission.GetProperty(ApplicationValidationFields.InstalledOtpRoot).GetString()!;
        foreach (JsonElement entry in root.GetProperty(ApplicationValidationFields.Inputs).EnumerateArray())
        {
            if (Hash(Path.Combine(installed, entry.GetProperty(ApplicationValidationFields.Path).GetString()!)) != entry.GetProperty(ApplicationValidationFields.Sha256).GetString())
            {
                throw new InvalidDataException(ApplicationValidationDiagnostics.OracleChanged);
            }
        }
    }
}
