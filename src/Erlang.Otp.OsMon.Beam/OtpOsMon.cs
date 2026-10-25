using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpOsMon
{
    private static readonly OtpOsMonManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static IReadOnlyList<string> SupportFiles { get; } = Array.AsReadOnly(Catalog.Resources.Keys
        .Where(name => name.StartsWith(OtpOsMonNames.SupportPrefix, StringComparison.Ordinal))
        .Select(name => name[OtpOsMonNames.SupportPrefix.Length..]).Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadSupportFile(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string key = OtpOsMonNames.SupportPrefix + name;
        if (!Catalog.Resources.TryGetValue(key, out string? hash))
            throw new ArgumentException(OtpOsMonNames.UnknownSupportFile, nameof(name));

        return ReadVerified(key, hash);
    }

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpOsMonNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpOsMonNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpOsMonNames.Application, Catalog.Resources[OtpOsMonNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpOsMonNames.Upgrade, Catalog.Resources[OtpOsMonNames.Upgrade]);

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
            throw new InvalidDataException(OtpOsMonNames.HashMismatch);

        return bytes;
    }

    private static OtpOsMonManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpOsMonNames.Catalog);

        return JsonSerializer.Deserialize<OtpOsMonManifest>(input)
            ?? throw new InvalidDataException(OtpOsMonNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpOsMon).Assembly.GetManifestResourceStream(OtpOsMonNames.Prefix + name)
            ?? throw new InvalidDataException(OtpOsMonNames.MissingResource);
}
