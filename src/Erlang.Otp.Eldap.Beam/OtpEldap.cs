using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpEldap
{
    private static readonly OtpEldapManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpEldapNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpEldapNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpEldapNames.Application, Catalog.Resources[OtpEldapNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpEldapNames.Upgrade, Catalog.Resources[OtpEldapNames.Upgrade]);

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
            throw new InvalidDataException(OtpEldapNames.HashMismatch);

        return bytes;
    }

    private static OtpEldapManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpEldapNames.Catalog);

        return JsonSerializer.Deserialize<OtpEldapManifest>(input)
            ?? throw new InvalidDataException(OtpEldapNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpEldap).Assembly.GetManifestResourceStream(OtpEldapNames.Prefix + name)
            ?? throw new InvalidDataException(OtpEldapNames.MissingResource);
}
