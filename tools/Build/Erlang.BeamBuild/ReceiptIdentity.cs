using System.Text.Json;

namespace Erlang.BeamBuild;

internal static class ReceiptIdentity
{
    internal static void RequireRoot(JsonElement proof, string root)
    {
        StringComparison paths = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!Path.GetFullPath(proof.GetProperty(BeamCompilerReceiptFields.InstalledOtpRoot).GetString()!).Equals(Path.GetFullPath(root), paths))
            throw new InvalidDataException(BeamBuildNames.Admission);
    }
}
