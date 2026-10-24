using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpTools
{
    private static readonly OtpToolsManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpToolsNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpToolsNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpToolsNames.Application, Catalog.Resources[OtpToolsNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpToolsNames.Upgrade, Catalog.Resources[OtpToolsNames.Upgrade]);

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
            throw new InvalidDataException(OtpToolsNames.HashMismatch);

        return bytes;
    }

    private static OtpToolsManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpToolsNames.Catalog);

        return JsonSerializer.Deserialize<OtpToolsManifest>(input)
            ?? throw new InvalidDataException(OtpToolsNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpTools).Assembly.GetManifestResourceStream(OtpToolsNames.Prefix + name)
            ?? throw new InvalidDataException(OtpToolsNames.MissingResource);
}
