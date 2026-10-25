using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpMegaco
{
    private static readonly OtpMegacoManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpMegacoNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpMegacoNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpMegacoNames.Application, Catalog.Resources[OtpMegacoNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpMegacoNames.Upgrade, Catalog.Resources[OtpMegacoNames.Upgrade]);

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
            throw new InvalidDataException(OtpMegacoNames.HashMismatch);

        return bytes;
    }

    private static OtpMegacoManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpMegacoNames.Catalog);

        return JsonSerializer.Deserialize<OtpMegacoManifest>(input)
            ?? throw new InvalidDataException(OtpMegacoNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpMegaco).Assembly.GetManifestResourceStream(OtpMegacoNames.Prefix + name)
            ?? throw new InvalidDataException(OtpMegacoNames.MissingResource);
}
