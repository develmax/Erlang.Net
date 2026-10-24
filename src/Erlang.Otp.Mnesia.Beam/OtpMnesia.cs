using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpMnesia
{
    private static readonly OtpMnesiaManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpMnesiaNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpMnesiaNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpMnesiaNames.Application, Catalog.Resources[OtpMnesiaNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpMnesiaNames.Upgrade, Catalog.Resources[OtpMnesiaNames.Upgrade]);

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
            throw new InvalidDataException(OtpMnesiaNames.HashMismatch);

        return bytes;
    }

    private static OtpMnesiaManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpMnesiaNames.Catalog);

        return JsonSerializer.Deserialize<OtpMnesiaManifest>(input)
            ?? throw new InvalidDataException(OtpMnesiaNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpMnesia).Assembly.GetManifestResourceStream(OtpMnesiaNames.Prefix + name)
            ?? throw new InvalidDataException(OtpMnesiaNames.MissingResource);
}
