using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Erlang;

// Root/package-owner creates this from the actual build/publish receipt. Guest
// load requests never select an alternate host program or change its arguments.
internal sealed class ErlDriverHostPackage420
{
    private readonly string root;
    private readonly ErlDriverHostPackageManifest420 manifest;

    private ErlDriverHostPackage420(string root, ErlDriverHostPackageManifest420 manifest)
    {
        this.root = root;
        this.manifest = manifest;
    }

    internal string EntryPoint => Resolve(manifest.EntryPoint);

    internal static ErlDriverHostPackage420 Read(string manifestPath, string expectedSha256)
    {
        string full = Path.GetFullPath(manifestPath);
        byte[] bytes = File.ReadAllBytes(full);
        if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(expectedSha256, StringComparison.Ordinal))
            throw new InvalidDataException(ErlDriverHostNames420.InvalidPackage);
        var data = JsonSerializer.Deserialize<ErlDriverHostPackageManifest420>(bytes, new JsonSerializerOptions
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        }) ?? throw new InvalidDataException(ErlDriverHostNames420.InvalidPackage);
        var package = new ErlDriverHostPackage420(Path.GetDirectoryName(full)!, data);
        package.Verify();

        return package;
    }

    internal void Verify()
    {
        if (!OperatingSystem.IsWindows() || manifest.Protocol != ErlDriverHostNames420.Version
            || manifest.Files is null || manifest.Files.Length == 0)
            throw new InvalidDataException(ErlDriverHostNames420.InvalidPackage);
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in manifest.Files)
        {
            string full = Resolve(file.Path);
            if (!paths.Add(full) || file.Sha256 is null || file.Sha256.Length != SHA256.HashSizeInBytes * 2)
                throw new InvalidDataException(ErlDriverHostNames420.InvalidPackage);
            using var stream = new FileStream(
                full,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read
            );
            if (!Convert.ToHexString(SHA256.HashData(stream)).Equals(file.Sha256, StringComparison.Ordinal))
                throw new InvalidDataException(ErlDriverHostNames420.InvalidPackage);
        }
        if (!paths.Contains(EntryPoint) || !Path.GetExtension(EntryPoint).Equals(ErlDriverHostNames420.ExecutableExtension, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(ErlDriverHostNames420.InvalidPackage);
    }

    private string Resolve(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path))
            throw new InvalidDataException(ErlDriverHostNames420.InvalidPackage);
        string full = Path.GetFullPath(Path.Combine(root, path));
        string relative = Path.GetRelativePath(root, full);
        if (relative == ErlDriverHostNames420.ParentDirectory || relative.StartsWith(ErlDriverHostNames420.ParentDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || Path.IsPathRooted(relative))
            throw new InvalidDataException(ErlDriverHostNames420.InvalidPackage);
        string current = root;
        foreach (string component in relative.Split(Path.DirectorySeparatorChar))
        {
            current = Path.Combine(current, component);
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException(ErlDriverHostNames420.InvalidPackage);
        }

        return full;
    }
}
