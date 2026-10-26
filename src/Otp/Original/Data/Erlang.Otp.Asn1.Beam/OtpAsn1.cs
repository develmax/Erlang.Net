using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpAsn1
{
    private static readonly OtpAsn1Manifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpAsn1Names.UnknownModule, nameof(module));

        return ReadVerified(module + OtpAsn1Names.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpAsn1Names.Application, Catalog.Resources[OtpAsn1Names.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpAsn1Names.Upgrade, Catalog.Resources[OtpAsn1Names.Upgrade]);

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
            throw new InvalidDataException(OtpAsn1Names.HashMismatch);

        return bytes;
    }

    private static OtpAsn1Manifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpAsn1Names.Catalog);

        return JsonSerializer.Deserialize<OtpAsn1Manifest>(input)
            ?? throw new InvalidDataException(OtpAsn1Names.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpAsn1).Assembly.GetManifestResourceStream(OtpAsn1Names.Prefix + name)
            ?? throw new InvalidDataException(OtpAsn1Names.MissingResource);
}
