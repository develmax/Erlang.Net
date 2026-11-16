using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

internal static class SupervisorAdmission
{
    internal static void Verify(string repository, string directory, string stdlib)
    {
        using JsonDocument provenance = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, SupervisorValidationPaths.Provenance)));
        JsonElement root = provenance.RootElement;
        foreach (JsonElement file in root.GetProperty("ReferenceSources").EnumerateArray())
        {
            Require(Hash(Path.Combine(repository, file.GetProperty("Path").GetString()!)) == file.GetProperty("Sha256").GetString());
        }
        foreach (JsonElement file in root.GetProperty("ReferenceBeams").EnumerateArray())
        {
            Require(Hash(Path.Combine(directory, file.GetProperty("Path").GetString()!)) == file.GetProperty("Sha256").GetString());
        }
        Require(root.GetProperty(SupervisorProvenanceFields.Baseline).GetString() == SupervisorValidationBaseline.Release
            && root.GetProperty(SupervisorProvenanceFields.Commit).GetString() == SupervisorValidationBaseline.Commit);

        void Check(string property, string path) => Require(Hash(path) == root.GetProperty(property).GetString());
        Check(
            SupervisorProvenanceFields.ObserverSourceSha256,
            Path.Combine(repository, SupervisorValidationPaths.ObserverSource)
        );
        Check(SupervisorProvenanceFields.ObserverBeamSha256, Path.Combine(directory, SupervisorValidationPaths.ObserverBeam));
        Check(
            SupervisorProvenanceFields.CompanionSourceSha256,
            Path.Combine(repository, SupervisorValidationPaths.CompanionSource)
        );
        Check(SupervisorProvenanceFields.CompanionBeamSha256, Path.Combine(directory, SupervisorValidationPaths.CompanionBeam));
        Check(SupervisorProvenanceFields.CorpusSha256, Path.Combine(directory, SupervisorValidationPaths.Corpus));
        Check(SupervisorProvenanceFields.EffectsSha256, Path.Combine(directory, SupervisorValidationPaths.Effects));
        Check(SupervisorProvenanceFields.ExportsSha256, Path.Combine(directory, SupervisorValidationPaths.Exports));
        Check(SupervisorProvenanceFields.FeatureBootstrapSha256, Path.Combine(directory, SupervisorValidationPaths.FeatureBootstrap));
        JsonElement original = root.GetProperty(SupervisorProvenanceFields.Inputs).EnumerateArray().Single(entry => entry.GetProperty(SupervisorProvenanceFields.Path).GetString()!.EndsWith(SupervisorValidationPaths.OriginalBeamInputSuffix, StringComparison.Ordinal));
        Require(Hash(Path.Combine(stdlib, SupervisorValidationPaths.OriginalBeam)) == original.GetProperty(SupervisorProvenanceFields.Sha256).GetString());
        Term declared = ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, SupervisorValidationPaths.Exports)));
        Require(Cons.Items(declared).Cast<TupleTerm>().Select(row => (((Atom)row.Items[0]).Name, (int)((Integer)row.Items[1]).Value)).ToHashSet().SetEquals(SupervisorModule.Exports));
    }

    internal static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static void Require(bool value)
    {
        if (!value)
        {
            throw new InvalidDataException(SupervisorValidationDiagnostics.AdmissionFailed);
        }
    }
}
