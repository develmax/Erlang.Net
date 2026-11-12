using System.Security.Cryptography;
using System.Text.Json;

internal static class SysAdmission
{
    internal static void Verify(string repository, string directory, IReadOnlyDictionary<string, string> modules)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, SysValidationFiles.Provenance)));
        JsonElement provenance = document.RootElement;
        Require(provenance.GetProperty(SysProvenanceFields.Baseline).GetString() == SysValidationBaseline.Tag
            && provenance.GetProperty(SysProvenanceFields.Commit).GetString() == SysValidationBaseline.Commit);
        Check(SysProvenanceFields.ObserverSourceSha256, Path.Combine(repository, SysValidationFiles.ObserverSource));
        Check(SysProvenanceFields.ObserverBeamSha256, Path.Combine(directory, SysValidationFiles.ObserverBeam));
        Check(SysProvenanceFields.ExportsSha256, Path.Combine(directory, SysValidationFiles.Exports));
        Check(SysProvenanceFields.FeaturesSha256, Path.Combine(directory, SysValidationFiles.Features));
        Check(SysProvenanceFields.CorpusSha256, Path.Combine(directory, SysValidationFiles.Corpus));
        foreach (JsonElement fixture in provenance.GetProperty(SysProvenanceFields.Fixtures).EnumerateArray())
        {
            Require(Hash(Path.Combine(repository, fixture.GetProperty(SysProvenanceFields.Source).GetString()!)) == fixture.GetProperty(SysProvenanceFields.SourceSha256).GetString());
            Require(Hash(Path.Combine(directory, fixture.GetProperty(SysProvenanceFields.Beam).GetString()!)) == fixture.GetProperty(SysProvenanceFields.BeamSha256).GetString());
        }
        JsonElement original = provenance.GetProperty(SysProvenanceFields.Inputs).EnumerateArray().Single(input => input.GetProperty(SysProvenanceFields.Path).GetString()!.EndsWith(SysValidationFiles.OriginalBeamSuffix, StringComparison.Ordinal));
        Require(Hash(modules[SysValidationModules.Sys]) == original.GetProperty(SysProvenanceFields.Sha256).GetString());
        JsonElement source = provenance.GetProperty(SysProvenanceFields.Inputs).EnumerateArray().Single(input => input.GetProperty(SysProvenanceFields.Path).GetString()!.EndsWith(SysValidationFiles.OriginalSourceSuffix, StringComparison.Ordinal));
        Require(Hash(Path.ChangeExtension(modules[SysValidationModules.Sys], SysValidationFiles.ErlangExtension)) == source.GetProperty(SysProvenanceFields.Sha256).GetString());

        void Check(string property, string path) => Require(Hash(path) == provenance.GetProperty(property).GetString());
    }

    internal static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static void Require(bool condition)
    {
        if (!condition)
        {
            throw new InvalidDataException(SysValidationDiagnostics.Admission);
        }
    }
}
