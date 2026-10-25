using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpWx
{
    private static readonly OtpWxManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static IReadOnlyList<string> SupportFiles { get; } = Array.AsReadOnly(Catalog.Resources.Keys
        .Where(name => name.StartsWith(OtpWxNames.SupportPrefix, StringComparison.Ordinal))
        .Select(name => name[OtpWxNames.SupportPrefix.Length..]).Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadSupportFile(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string key = OtpWxNames.SupportPrefix + name;
        if (!Catalog.Resources.TryGetValue(key, out string? hash))
            throw new ArgumentException(OtpWxNames.UnknownSupportFile, nameof(name));

        return ReadVerified(key, hash);
    }

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpWxNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpWxNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpWxNames.Application, Catalog.Resources[OtpWxNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpWxNames.Upgrade, Catalog.Resources[OtpWxNames.Upgrade]);

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
            throw new InvalidDataException(OtpWxNames.HashMismatch);

        return bytes;
    }

    private static OtpWxManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpWxNames.Catalog);

        return JsonSerializer.Deserialize<OtpWxManifest>(input)
            ?? throw new InvalidDataException(OtpWxNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpWx).Assembly.GetManifestResourceStream(OtpWxNames.Prefix + name)
            ?? throw new InvalidDataException(OtpWxNames.MissingResource);
}
