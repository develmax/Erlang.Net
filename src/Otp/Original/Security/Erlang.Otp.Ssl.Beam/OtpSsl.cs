using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpSsl
{
    private static readonly OtpSslManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpSslNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpSslNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpSslNames.Application, Catalog.Resources[OtpSslNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpSslNames.Upgrade, Catalog.Resources[OtpSslNames.Upgrade]);

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
            throw new InvalidDataException(OtpSslNames.HashMismatch);

        return bytes;
    }

    private static OtpSslManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpSslNames.Catalog);

        return JsonSerializer.Deserialize<OtpSslManifest>(input)
            ?? throw new InvalidDataException(OtpSslNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpSsl).Assembly.GetManifestResourceStream(OtpSslNames.Prefix + name)
            ?? throw new InvalidDataException(OtpSslNames.MissingResource);
}
