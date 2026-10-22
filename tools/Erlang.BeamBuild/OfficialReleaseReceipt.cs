using System.Text.Json;

namespace Erlang.BeamBuild;

internal static class OfficialReleaseReceipt
{
    internal static string Validate(JsonElement proof, string root)
    {
        ReceiptIdentity.RequireRoot(proof, root);
        if (proof.GetProperty(BeamCompilerReceiptFields.Provider).GetString() != BeamBuildProvenanceNames.OfficialRelease
            || proof.GetProperty(BeamCompilerReceiptFields.ReleaseTag).GetString() != BeamBuildNames.Tag
            || proof.GetProperty(BeamCompilerReceiptFields.SourceCommit).GetString() != BeamBuildNames.Commit
            || proof.GetProperty(BeamCompilerReceiptFields.ReleaseVersion).GetString() != BeamBuildNames.Version
            || !proof.GetProperty(BeamCompilerReceiptFields.ReleaseUrl).GetString()!.StartsWith(BeamOfficialReleaseNames.DownloadPrefix, StringComparison.Ordinal)
            || proof.TryGetProperty(BeamCompilerReceiptFields.SourceBuildCompleted, out _))
            throw new InvalidDataException(BeamBuildNames.Admission);
        string archive = proof.GetProperty(BeamCompilerReceiptFields.ArchivePath).GetString()!;
        if (!Path.IsPathFullyQualified(archive)
            || !BeamBuildStorage.HashFile(archive).Equals(proof.GetProperty(BeamCompilerReceiptFields.ArchiveSha256).GetString(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(BeamBuildNames.Admission);

        // This is archive identity, deliberately not a source-build log identity.
        return BeamBuildStorage.HashFile(archive);
    }
}
