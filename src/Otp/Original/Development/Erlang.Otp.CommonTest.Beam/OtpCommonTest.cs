using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpCommonTest
{
    private static readonly OtpCommonTestManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static IReadOnlyList<string> SupportFiles { get; } = Array.AsReadOnly(Catalog.Resources.Keys
        .Where(name => name.StartsWith(OtpCommonTestNames.SupportPrefix, StringComparison.Ordinal))
        .Select(name => name[OtpCommonTestNames.SupportPrefix.Length..]).Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadSupportFile(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string key = OtpCommonTestNames.SupportPrefix + name;
        if (!Catalog.Resources.TryGetValue(key, out string? hash))
            throw new ArgumentException(OtpCommonTestNames.UnknownSupportFile, nameof(name));

        return ReadVerified(key, hash);
    }

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpCommonTestNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpCommonTestNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpCommonTestNames.Application, Catalog.Resources[OtpCommonTestNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpCommonTestNames.Upgrade, Catalog.Resources[OtpCommonTestNames.Upgrade]);

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
            throw new InvalidDataException(OtpCommonTestNames.HashMismatch);

        return bytes;
    }

    private static OtpCommonTestManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpCommonTestNames.Catalog);

        return JsonSerializer.Deserialize<OtpCommonTestManifest>(input)
            ?? throw new InvalidDataException(OtpCommonTestNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpCommonTest).Assembly.GetManifestResourceStream(OtpCommonTestNames.Prefix + name)
            ?? throw new InvalidDataException(OtpCommonTestNames.MissingResource);
}
