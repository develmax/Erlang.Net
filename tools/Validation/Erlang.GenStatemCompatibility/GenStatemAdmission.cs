using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

internal static class GenStatemAdmission
{
    internal static void Verify(string repository, string directory, string stdlib)
    {
        using JsonDocument provenance = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, GenStatemValidationPaths.Provenance)));
        JsonElement root = provenance.RootElement;
        Require(root.GetProperty(GenStatemProvenanceFields.Baseline).GetString() == GenStatemValidationBaseline.Release
            && root.GetProperty(GenStatemProvenanceFields.Commit).GetString() == GenStatemValidationBaseline.Commit);

        void Check(string property, string path) => Require(Hash(path) == root.GetProperty(property).GetString());
        Check(
            GenStatemProvenanceFields.ObserverSourceSha256,
            Path.Combine(repository, GenStatemValidationPaths.ObserverSource)
        );
        Check(GenStatemProvenanceFields.ObserverBeamSha256, Path.Combine(directory, GenStatemValidationPaths.ObserverBeam));
        Check(
            GenStatemProvenanceFields.CompanionSourceSha256,
            Path.Combine(repository, GenStatemValidationPaths.CompanionSource)
        );
        Check(GenStatemProvenanceFields.CompanionBeamSha256, Path.Combine(directory, GenStatemValidationPaths.CompanionBeam));
        Check(GenStatemProvenanceFields.CorpusSha256, Path.Combine(directory, GenStatemValidationPaths.Corpus));
        Check(GenStatemProvenanceFields.EffectsSha256, Path.Combine(directory, GenStatemValidationPaths.Effects));
        Check(GenStatemProvenanceFields.ExportsSha256, Path.Combine(directory, GenStatemValidationPaths.Exports));
        Check(GenStatemProvenanceFields.FeatureBootstrapSha256, Path.Combine(directory, GenStatemValidationPaths.FeatureBootstrap));
        JsonElement original = root.GetProperty(GenStatemProvenanceFields.Inputs).EnumerateArray().Single(entry => entry.GetProperty(GenStatemProvenanceFields.Path).GetString()!.EndsWith(GenStatemValidationPaths.OriginalBeamInputSuffix, StringComparison.Ordinal));
        Require(Hash(Path.Combine(stdlib, GenStatemValidationPaths.OriginalBeam)) == original.GetProperty(GenStatemProvenanceFields.Sha256).GetString());
        Term declared = ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, GenStatemValidationPaths.Exports)));
        Require(Cons.Items(declared).Cast<TupleTerm>().Select(row => (((Atom)row.Items[0]).Name, (int)((Integer)row.Items[1]).Value)).ToHashSet().SetEquals(GenStatemModule.Exports));
    }

    internal static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static void Require(bool value)
    {
        if (!value)
        {
            throw new InvalidDataException(GenStatemValidationDiagnostics.AdmissionFailed);
        }
    }
}
