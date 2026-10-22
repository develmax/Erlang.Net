using System.Text.Json;

namespace Erlang.BeamBuild;

internal static class SourceBuildReceipt
{
    internal static string Validate(JsonElement proof, string root)
    {
        ReceiptIdentity.RequireRoot(proof, root);
        if (proof.GetProperty(BeamCompilerReceiptFields.SourceCommit).GetString() != BeamBuildNames.Commit
            || proof.GetProperty(BeamCompilerReceiptFields.SourceTag).GetString() != BeamBuildNames.Tag
            || !proof.GetProperty(BeamCompilerReceiptFields.SourceBuildCompleted).GetBoolean())
            throw new InvalidDataException(BeamBuildNames.Admission);
        string log = proof.GetProperty(BeamCompilerReceiptFields.SourceBuildLog).GetString()!;
        if (!Path.IsPathFullyQualified(log))
            throw new InvalidDataException(BeamBuildNames.Admission);
        string hash = BeamBuildStorage.HashFile(log);
        if (!hash.Equals(proof.GetProperty(BeamCompilerReceiptFields.SourceBuildLogSha256).GetString(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(BeamBuildNames.Admission);

        return hash;
    }
}
