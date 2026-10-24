using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpErts
{
    private static readonly OtpErtsManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpErtsNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpErtsNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpErtsNames.Application, Catalog.Resources[OtpErtsNames.Application]);

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
            throw new InvalidDataException(OtpErtsNames.HashMismatch);

        return bytes;
    }

    private static OtpErtsManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpErtsNames.Catalog);

        return JsonSerializer.Deserialize<OtpErtsManifest>(input)
            ?? throw new InvalidDataException(OtpErtsNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpErts).Assembly.GetManifestResourceStream(OtpErtsNames.Prefix + name)
            ?? throw new InvalidDataException(OtpErtsNames.MissingResource);
}
