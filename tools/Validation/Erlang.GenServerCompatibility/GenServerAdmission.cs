using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

internal static class GenServerAdmission
{
    internal static void Verify(string repository, string directory, string stdlib)
    {
        using JsonDocument provenance = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, GenServerValidationPaths.Provenance)));
        JsonElement root = provenance.RootElement;
        Require(root.GetProperty(GenServerProvenanceFields.Baseline).GetString() == GenServerValidationBaseline.Release
            && root.GetProperty(GenServerProvenanceFields.Commit).GetString() == GenServerValidationBaseline.Commit);

        void Check(string property, string path) => Require(Hash(path) == root.GetProperty(property).GetString());
        Check(
            GenServerProvenanceFields.ObserverSourceSha256,
            Path.Combine(repository, GenServerValidationPaths.ObserverSource)
        );
        Check(GenServerProvenanceFields.ObserverBeamSha256, Path.Combine(directory, GenServerValidationPaths.ObserverBeam));
        Check(
            GenServerProvenanceFields.CompanionSourceSha256,
            Path.Combine(repository, GenServerValidationPaths.CompanionSource)
        );
        Check(GenServerProvenanceFields.CompanionBeamSha256, Path.Combine(directory, GenServerValidationPaths.CompanionBeam));
        Check(GenServerProvenanceFields.CorpusSha256, Path.Combine(directory, GenServerValidationPaths.Corpus));
        Check(GenServerProvenanceFields.EffectsSha256, Path.Combine(directory, GenServerValidationPaths.Effects));
        Check(GenServerProvenanceFields.ExportsSha256, Path.Combine(directory, GenServerValidationPaths.Exports));
        Check(GenServerProvenanceFields.FeatureBootstrapSha256, Path.Combine(directory, GenServerValidationPaths.FeatureBootstrap));
        JsonElement original = root.GetProperty(GenServerProvenanceFields.Inputs).EnumerateArray().Single(entry => entry.GetProperty(GenServerProvenanceFields.Path).GetString()!.EndsWith(GenServerValidationPaths.OriginalBeamInputSuffix, StringComparison.Ordinal));
        Require(Hash(Path.Combine(stdlib, GenServerValidationPaths.OriginalBeam)) == original.GetProperty(GenServerProvenanceFields.Sha256).GetString());
        Require(Hash(Path.Combine(repository, GenServerValidationPaths.UpstreamSource)) == root.GetProperty(GenServerProvenanceFields.SuiteSourceSha256).GetString());
        Require(Hash(Path.Combine(directory, GenServerValidationPaths.UpstreamBeam)) == root.GetProperty(GenServerProvenanceFields.SuiteBeamSha256).GetString());
        Term declared = ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, GenServerValidationPaths.Exports)));
        Require(Cons.Items(declared).Cast<TupleTerm>().Select(row => (((Atom)row.Items[0]).Name, (int)((Integer)row.Items[1]).Value)).ToHashSet().SetEquals(GenServerModule.Exports));
    }

    internal static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static void Require(bool value)
    {
        if (!value)
        {
            throw new InvalidDataException(GenServerValidationDiagnostics.AdmissionFailed);
        }
    }
}
