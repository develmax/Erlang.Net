using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpCrypto
{
    private static readonly OtpCryptoManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpCryptoNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpCryptoNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpCryptoNames.Application, Catalog.Resources[OtpCryptoNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpCryptoNames.Upgrade, Catalog.Resources[OtpCryptoNames.Upgrade]);

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
            throw new InvalidDataException(OtpCryptoNames.HashMismatch);

        return bytes;
    }

    private static OtpCryptoManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpCryptoNames.Catalog);

        return JsonSerializer.Deserialize<OtpCryptoManifest>(input)
            ?? throw new InvalidDataException(OtpCryptoNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpCrypto).Assembly.GetManifestResourceStream(OtpCryptoNames.Prefix + name)
            ?? throw new InvalidDataException(OtpCryptoNames.MissingResource);
}
