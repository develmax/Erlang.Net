using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpTftp
{
    private static readonly OtpTftpManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpTftpNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpTftpNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpTftpNames.Application, Catalog.Resources[OtpTftpNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpTftpNames.Upgrade, Catalog.Resources[OtpTftpNames.Upgrade]);

    public static RuntimeModuleSelection Beam(
        string module,
        IEnumerable<string> dependencies
    )
    {
        ArgumentNullException.ThrowIfNull(dependencies);

        return RuntimeModuleSelection.Beam(module, ReadImage(module), dependencies);
    }

    private static byte[] ReadVerified(string name, string hash)
    {
        using Stream input = OpenResource(name);
        using var output = new MemoryStream();
        input.CopyTo(output);
        byte[] bytes = output.ToArray();
        if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(hash, StringComparison.Ordinal))
            throw new InvalidDataException(OtpTftpNames.HashMismatch);

        return bytes;
    }

    private static OtpTftpManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpTftpNames.Catalog);

        return JsonSerializer.Deserialize<OtpTftpManifest>(input)
            ?? throw new InvalidDataException(OtpTftpNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpTftp).Assembly.GetManifestResourceStream(OtpTftpNames.Prefix + name)
            ?? throw new InvalidDataException(OtpTftpNames.MissingResource);
}
