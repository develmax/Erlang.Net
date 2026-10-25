using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpOdbc
{
    private static readonly OtpOdbcManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static IReadOnlyList<string> SupportFiles { get; } = Array.AsReadOnly(Catalog.Resources.Keys
        .Where(name => name.StartsWith(OtpOdbcNames.SupportPrefix, StringComparison.Ordinal))
        .Select(name => name[OtpOdbcNames.SupportPrefix.Length..]).Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadSupportFile(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string key = OtpOdbcNames.SupportPrefix + name;
        if (!Catalog.Resources.TryGetValue(key, out string? hash))
            throw new ArgumentException(OtpOdbcNames.UnknownSupportFile, nameof(name));

        return ReadVerified(key, hash);
    }

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpOdbcNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpOdbcNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpOdbcNames.Application, Catalog.Resources[OtpOdbcNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpOdbcNames.Upgrade, Catalog.Resources[OtpOdbcNames.Upgrade]);

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
            throw new InvalidDataException(OtpOdbcNames.HashMismatch);

        return bytes;
    }

    private static OtpOdbcManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpOdbcNames.Catalog);

        return JsonSerializer.Deserialize<OtpOdbcManifest>(input)
            ?? throw new InvalidDataException(OtpOdbcNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpOdbc).Assembly.GetManifestResourceStream(OtpOdbcNames.Prefix + name)
            ?? throw new InvalidDataException(OtpOdbcNames.MissingResource);
}
