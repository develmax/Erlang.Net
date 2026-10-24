using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpXmerl
{
    private static readonly OtpXmerlManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpXmerlNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpXmerlNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpXmerlNames.Application, Catalog.Resources[OtpXmerlNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpXmerlNames.Upgrade, Catalog.Resources[OtpXmerlNames.Upgrade]);

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
            throw new InvalidDataException(OtpXmerlNames.HashMismatch);

        return bytes;
    }

    private static OtpXmerlManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpXmerlNames.Catalog);

        return JsonSerializer.Deserialize<OtpXmerlManifest>(input)
            ?? throw new InvalidDataException(OtpXmerlNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpXmerl).Assembly.GetManifestResourceStream(OtpXmerlNames.Prefix + name)
            ?? throw new InvalidDataException(OtpXmerlNames.MissingResource);
}
