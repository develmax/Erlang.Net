using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpEdoc
{
    private static readonly OtpEdocManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static IReadOnlyList<string> SupportFiles { get; } = Array.AsReadOnly(Catalog.Resources.Keys
        .Where(name => name.StartsWith(OtpEdocNames.SupportPrefix, StringComparison.Ordinal))
        .Select(name => name[OtpEdocNames.SupportPrefix.Length..]).Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadSupportFile(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string key = OtpEdocNames.SupportPrefix + name;
        if (!Catalog.Resources.TryGetValue(key, out string? hash))
            throw new ArgumentException(OtpEdocNames.UnknownSupportFile, nameof(name));

        return ReadVerified(key, hash);
    }

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpEdocNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpEdocNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpEdocNames.Application, Catalog.Resources[OtpEdocNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpEdocNames.Upgrade, Catalog.Resources[OtpEdocNames.Upgrade]);

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
            throw new InvalidDataException(OtpEdocNames.HashMismatch);

        return bytes;
    }

    private static OtpEdocManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpEdocNames.Catalog);

        return JsonSerializer.Deserialize<OtpEdocManifest>(input)
            ?? throw new InvalidDataException(OtpEdocNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpEdoc).Assembly.GetManifestResourceStream(OtpEdocNames.Prefix + name)
            ?? throw new InvalidDataException(OtpEdocNames.MissingResource);
}
