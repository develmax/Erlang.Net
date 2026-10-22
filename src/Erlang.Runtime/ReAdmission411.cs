using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang;

internal static class ReAdmission411
{
    internal static string Validate(string admissionPath)
    {
        using var admission = JsonDocument.Parse(File.ReadAllBytes(admissionPath));
        var root = admission.RootElement;
        if (root.GetProperty(ReProtocolNames411.SourceCommit).GetString() != ReProtocolNames411.PinnedCommit || root.GetProperty(ReProtocolNames411.ReleaseTag).GetString() != ReProtocolNames411.OTP2911)
            throw new InvalidOperationException(ReDiagnosticNames411.ForeignOriginalProvider);
        var folder = Path.GetFullPath(root.GetProperty(ReProtocolNames411.InstalledOtpRoot).GetString()!);
        var manifestPath = root.GetProperty(ReProtocolNames411.ExecutionManifestPath).GetString()!;
        if (Hash(manifestPath) != root.GetProperty(ReProtocolNames411.ExecutionManifestSha256).GetString()) throw new InvalidOperationException(ReDiagnosticNames411.OriginalManifestChanged);
        using var manifest = JsonDocument.Parse(File.ReadAllBytes(manifestPath));
        if (manifest.RootElement.GetArrayLength() != 6427) throw new InvalidOperationException(ReDiagnosticNames411.OriginalClosureCountChanged);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in manifest.RootElement.EnumerateArray())
        {
            string relative = row.GetProperty(ReProtocolNames411.Path).GetString()!;
            string file = Path.GetFullPath(Path.Combine(folder, relative));
            if (!file.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !seen.Add(file) || Hash(file) != row.GetProperty(ReProtocolNames411.Sha256).GetString())
                throw new InvalidOperationException(ReDiagnosticNames411.OriginalClosureChanged);
        }
        if (!Directory.EnumerateFiles(folder, ReProtocolNames411.AllFilesPattern, SearchOption.AllDirectories).All(file => seen.Contains(Path.GetFullPath(file))))
            throw new InvalidOperationException(ReDiagnosticNames411.UnexpectedOriginalClosureFile);

        return Path.Combine(
            folder,
            ReProtocolNames411.Erts171,
            ReProtocolNames411.Bin,
            ReProtocolNames411.BeamSmpDll
        );
    }

    private static string Hash(string file)
    {
        using var stream = File.OpenRead(file);

        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
