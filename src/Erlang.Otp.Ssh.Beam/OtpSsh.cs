using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpSsh
{
    private static readonly OtpSshManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpSshNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpSshNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpSshNames.Application, Catalog.Resources[OtpSshNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpSshNames.Upgrade, Catalog.Resources[OtpSshNames.Upgrade]);

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
            throw new InvalidDataException(OtpSshNames.HashMismatch);

        return bytes;
    }

    private static OtpSshManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpSshNames.Catalog);

        return JsonSerializer.Deserialize<OtpSshManifest>(input)
            ?? throw new InvalidDataException(OtpSshNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpSsh).Assembly.GetManifestResourceStream(OtpSshNames.Prefix + name)
            ?? throw new InvalidDataException(OtpSshNames.MissingResource);
}
