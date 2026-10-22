using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Erlang;

internal sealed record ErlDriverServerConfiguration420(
    string AbiFile,
    string AbiSha256,
    string ErrnoFile,
    string ErrnoSha256,
    string BridgeFile,
    string BridgeSha256
)
{
    internal static ErlDriverServerConfiguration420 Read()
        => JsonSerializer.Deserialize<ErlDriverServerConfiguration420>(
            File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, ErlDriverHostNames420.ConfigurationFile)),
            new JsonSerializerOptions { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow }
        )
            ?? throw new InvalidDataException(ErlDriverHostNames420.InvalidPackage);

    private static string Resolve(string relative)
    {
        if (Path.IsPathRooted(relative)) throw new InvalidDataException(ErlDriverHostNames420.InvalidPackage);
        string root = Path.GetFullPath(AppContext.BaseDirectory);
        string path = Path.GetFullPath(Path.Combine(root, relative));
        if (!path.StartsWith(root + (Path.EndsInDirectorySeparator(root) ? string.Empty : Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(ErlDriverHostNames420.InvalidPackage);
        for (string? current = path; current is not null && !current.Equals(root, StringComparison.OrdinalIgnoreCase); current = Path.GetDirectoryName(current))
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException(ErlDriverHostNames420.InvalidPackage);

        return path;
    }

    private static byte[] Bytes(string relative, string sha256)
    {
        byte[] bytes = File.ReadAllBytes(Resolve(relative));
        if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(sha256, StringComparison.Ordinal))
            throw new InvalidDataException(ErlDriverHostNames420.InvalidPackage);

        return bytes;
    }

    internal ErlDriverEngineInput420 Input(ErlDriverSystemInfo422 systemInfo)
    {
        byte[] abi = Bytes(AbiFile, AbiSha256);
        var errno = JsonSerializer.Deserialize<Dictionary<int, string>>(Bytes(ErrnoFile, ErrnoSha256))
            ?? throw new InvalidDataException(ErlDriverHostNames420.InvalidPackage);

        // Engine's native bridge owner rechecks the actual DLL hash while it
        // holds its read stream through NativeLibrary.Load.
        return new(
            abi,
            AbiSha256,
            errno,
            Resolve(BridgeFile),
            BridgeSha256,
            systemInfo
        );
    }
}
