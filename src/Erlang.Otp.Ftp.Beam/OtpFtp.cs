using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpFtp
{
    private static readonly OtpFtpManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpFtpNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpFtpNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpFtpNames.Application, Catalog.Resources[OtpFtpNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpFtpNames.Upgrade, Catalog.Resources[OtpFtpNames.Upgrade]);

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
            throw new InvalidDataException(OtpFtpNames.HashMismatch);

        return bytes;
    }

    private static OtpFtpManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpFtpNames.Catalog);

        return JsonSerializer.Deserialize<OtpFtpManifest>(input)
            ?? throw new InvalidDataException(OtpFtpNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpFtp).Assembly.GetManifestResourceStream(OtpFtpNames.Prefix + name)
            ?? throw new InvalidDataException(OtpFtpNames.MissingResource);
}
