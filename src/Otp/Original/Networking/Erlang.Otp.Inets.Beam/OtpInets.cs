using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpInets
{
    private static readonly OtpInetsManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpInetsNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpInetsNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpInetsNames.Application, Catalog.Resources[OtpInetsNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpInetsNames.Upgrade, Catalog.Resources[OtpInetsNames.Upgrade]);

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
            throw new InvalidDataException(OtpInetsNames.HashMismatch);

        return bytes;
    }

    private static OtpInetsManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpInetsNames.Catalog);

        return JsonSerializer.Deserialize<OtpInetsManifest>(input)
            ?? throw new InvalidDataException(OtpInetsNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpInets).Assembly.GetManifestResourceStream(OtpInetsNames.Prefix + name)
            ?? throw new InvalidDataException(OtpInetsNames.MissingResource);
}
