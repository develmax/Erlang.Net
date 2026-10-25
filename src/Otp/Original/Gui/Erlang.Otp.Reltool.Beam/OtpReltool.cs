using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpReltool
{
    private static readonly OtpReltoolManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static IReadOnlyList<string> SupportFiles { get; } = Array.AsReadOnly(Catalog.Resources.Keys
        .Where(name => name.StartsWith(OtpReltoolNames.SupportPrefix, StringComparison.Ordinal))
        .Select(name => name[OtpReltoolNames.SupportPrefix.Length..]).Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadSupportFile(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string key = OtpReltoolNames.SupportPrefix + name;
        if (!Catalog.Resources.TryGetValue(key, out string? hash))
            throw new ArgumentException(OtpReltoolNames.UnknownSupportFile, nameof(name));

        return ReadVerified(key, hash);
    }

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpReltoolNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpReltoolNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpReltoolNames.Application, Catalog.Resources[OtpReltoolNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpReltoolNames.Upgrade, Catalog.Resources[OtpReltoolNames.Upgrade]);

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
            throw new InvalidDataException(OtpReltoolNames.HashMismatch);

        return bytes;
    }

    private static OtpReltoolManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpReltoolNames.Catalog);

        return JsonSerializer.Deserialize<OtpReltoolManifest>(input)
            ?? throw new InvalidDataException(OtpReltoolNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpReltool).Assembly.GetManifestResourceStream(OtpReltoolNames.Prefix + name)
            ?? throw new InvalidDataException(OtpReltoolNames.MissingResource);
}
