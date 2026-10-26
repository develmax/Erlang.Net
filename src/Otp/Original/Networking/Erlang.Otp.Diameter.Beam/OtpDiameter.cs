using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpDiameter
{
    private static readonly OtpDiameterManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static IReadOnlyList<string> SupportFiles { get; } = Array.AsReadOnly(Catalog.Resources.Keys
        .Where(name => name.StartsWith(OtpDiameterNames.SupportPrefix, StringComparison.Ordinal))
        .Select(name => name[OtpDiameterNames.SupportPrefix.Length..]).Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadSupportFile(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string key = OtpDiameterNames.SupportPrefix + name;
        if (!Catalog.Resources.TryGetValue(key, out string? hash))
            throw new ArgumentException(OtpDiameterNames.UnknownSupportFile, nameof(name));

        return ReadVerified(key, hash);
    }

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpDiameterNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpDiameterNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpDiameterNames.Application, Catalog.Resources[OtpDiameterNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpDiameterNames.Upgrade, Catalog.Resources[OtpDiameterNames.Upgrade]);

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
            throw new InvalidDataException(OtpDiameterNames.HashMismatch);

        return bytes;
    }

    private static OtpDiameterManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpDiameterNames.Catalog);

        return JsonSerializer.Deserialize<OtpDiameterManifest>(input)
            ?? throw new InvalidDataException(OtpDiameterNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpDiameter).Assembly.GetManifestResourceStream(OtpDiameterNames.Prefix + name)
            ?? throw new InvalidDataException(OtpDiameterNames.MissingResource);
}
