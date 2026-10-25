using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpSnmp
{
    private static readonly OtpSnmpManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static IReadOnlyList<string> SupportFiles { get; } = Array.AsReadOnly(Catalog.Resources.Keys
        .Where(name => name.StartsWith(OtpSnmpNames.SupportPrefix, StringComparison.Ordinal))
        .Select(name => name[OtpSnmpNames.SupportPrefix.Length..]).Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadSupportFile(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string key = OtpSnmpNames.SupportPrefix + name;
        if (!Catalog.Resources.TryGetValue(key, out string? hash))
            throw new ArgumentException(OtpSnmpNames.UnknownSupportFile, nameof(name));

        return ReadVerified(key, hash);
    }

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpSnmpNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpSnmpNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpSnmpNames.Application, Catalog.Resources[OtpSnmpNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpSnmpNames.Upgrade, Catalog.Resources[OtpSnmpNames.Upgrade]);

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
            throw new InvalidDataException(OtpSnmpNames.HashMismatch);

        return bytes;
    }

    private static OtpSnmpManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpSnmpNames.Catalog);

        return JsonSerializer.Deserialize<OtpSnmpManifest>(input)
            ?? throw new InvalidDataException(OtpSnmpNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpSnmp).Assembly.GetManifestResourceStream(OtpSnmpNames.Prefix + name)
            ?? throw new InvalidDataException(OtpSnmpNames.MissingResource);
}
