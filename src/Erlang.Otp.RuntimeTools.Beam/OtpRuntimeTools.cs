using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpRuntimeTools
{
    private static readonly OtpRuntimeToolsManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpRuntimeToolsNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpRuntimeToolsNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpRuntimeToolsNames.Application, Catalog.Resources[OtpRuntimeToolsNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpRuntimeToolsNames.Upgrade, Catalog.Resources[OtpRuntimeToolsNames.Upgrade]);

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
            throw new InvalidDataException(OtpRuntimeToolsNames.HashMismatch);

        return bytes;
    }

    private static OtpRuntimeToolsManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpRuntimeToolsNames.Catalog);

        return JsonSerializer.Deserialize<OtpRuntimeToolsManifest>(input)
            ?? throw new InvalidDataException(OtpRuntimeToolsNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpRuntimeTools).Assembly.GetManifestResourceStream(OtpRuntimeToolsNames.Prefix + name)
            ?? throw new InvalidDataException(OtpRuntimeToolsNames.MissingResource);
}
