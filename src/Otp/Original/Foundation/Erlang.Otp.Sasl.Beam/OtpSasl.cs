using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpSasl
{
    private static readonly OtpSaslManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpSaslNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpSaslNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpSaslNames.Application, Catalog.Resources[OtpSaslNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpSaslNames.Upgrade, Catalog.Resources[OtpSaslNames.Upgrade]);

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
            throw new InvalidDataException(OtpSaslNames.HashMismatch);

        return bytes;
    }

    private static OtpSaslManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpSaslNames.Catalog);

        return JsonSerializer.Deserialize<OtpSaslManifest>(input)
            ?? throw new InvalidDataException(OtpSaslNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpSasl).Assembly.GetManifestResourceStream(OtpSaslNames.Prefix + name)
            ?? throw new InvalidDataException(OtpSaslNames.MissingResource);
}
