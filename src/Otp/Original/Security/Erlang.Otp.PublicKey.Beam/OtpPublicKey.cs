using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpPublicKey
{
    private static readonly OtpPublicKeyManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpPublicKeyNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpPublicKeyNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpPublicKeyNames.Application, Catalog.Resources[OtpPublicKeyNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpPublicKeyNames.Upgrade, Catalog.Resources[OtpPublicKeyNames.Upgrade]);

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
            throw new InvalidDataException(OtpPublicKeyNames.HashMismatch);

        return bytes;
    }

    private static OtpPublicKeyManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpPublicKeyNames.Catalog);

        return JsonSerializer.Deserialize<OtpPublicKeyManifest>(input)
            ?? throw new InvalidDataException(OtpPublicKeyNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpPublicKey).Assembly.GetManifestResourceStream(OtpPublicKeyNames.Prefix + name)
            ?? throw new InvalidDataException(OtpPublicKeyNames.MissingResource);
}
