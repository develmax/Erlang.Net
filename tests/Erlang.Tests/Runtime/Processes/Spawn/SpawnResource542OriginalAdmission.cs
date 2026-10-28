using System.Security.Cryptography;
using System.Text.Json;
using static Erlang.Tests.SpawnResource541Names;
using static Erlang.Tests.SpawnResource542Names;

namespace Erlang.Tests;

internal static class SpawnResource542OriginalAdmission
{
    internal static (byte[] Bytes, string Path, string Sha256) Read(JsonElement providerBefore)
    {
        string admissionPath = providerBefore.GetProperty(AdmissionPath).GetString()!;
        Require(System.IO.Path.IsPathFullyQualified(admissionPath)
            && Digest(admissionPath) == providerBefore.GetProperty(AdmissionSha).GetString());
        using var document = JsonDocument.Parse(File.ReadAllBytes(admissionPath));
        JsonElement admission = document.RootElement;
        Require(admission.GetProperty(SourceCommit).GetString() == Pin
            && admission.GetProperty(ReleaseTag).GetString() == Release
            && admission.GetProperty(FullArchive).GetBoolean());
        string root = System.IO.Path.GetFullPath(admission.GetProperty(InstalledRoot).GetString()!);
        JsonElement inventory = providerBefore.GetProperty(SpawnResource542Names.Inventory);
        var rows = inventory.EnumerateObject().ToArray();
        Require(rows.Length == OfficialFileCount);
        var candidates = rows.Where(row => row.Name.Replace('\\', '/').StartsWith(LibraryPrefix, StringComparison.Ordinal)
            && row.Name.Replace('\\', '/').EndsWith(OriginalSuffix, StringComparison.Ordinal)).ToArray();
        Require(candidates.Length == 1);
        string relative = candidates[0].Name.Replace('\\', '/');
        Require(!System.IO.Path.IsPathRooted(relative));
        string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(root, relative));
        Require(path.StartsWith(root + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
        byte[] bytes = File.ReadAllBytes(path);
        string sha = Convert.ToHexString(SHA256.HashData(bytes));
        Require(sha == candidates[0].Value.GetString());
        string[] imports = DynamicApplyBoundary520Imports.Read(bytes, new EtfDecodeOptions
        {
            ExternalFunctionFactory = (identity, _) => new FunctionTerm(identity, RefuseFunction)
        });
        Require(imports.SequenceEqual(Imports));

        return (bytes, path, sha);
    }

    private static ValueTask<Term> RefuseFunction(ITermExecutionContext context, IReadOnlyList<Term> arguments)
        => throw new InvalidOperationException(SpawnResource542Names.PassiveFunction);

    private static string Digest(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidDataException(Failure);
    }
}
