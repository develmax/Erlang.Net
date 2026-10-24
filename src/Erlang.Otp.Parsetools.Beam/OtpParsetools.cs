using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpParsetools
{
    private static readonly OtpParsetoolsManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpParsetoolsNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpParsetoolsNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpParsetoolsNames.Application, Catalog.Resources[OtpParsetoolsNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpParsetoolsNames.Upgrade, Catalog.Resources[OtpParsetoolsNames.Upgrade]);

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
            throw new InvalidDataException(OtpParsetoolsNames.HashMismatch);

        return bytes;
    }

    private static OtpParsetoolsManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpParsetoolsNames.Catalog);

        return JsonSerializer.Deserialize<OtpParsetoolsManifest>(input)
            ?? throw new InvalidDataException(OtpParsetoolsNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpParsetools).Assembly.GetManifestResourceStream(OtpParsetoolsNames.Prefix + name)
            ?? throw new InvalidDataException(OtpParsetoolsNames.MissingResource);
}
