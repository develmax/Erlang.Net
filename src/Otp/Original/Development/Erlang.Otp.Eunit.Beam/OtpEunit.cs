using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpEunit
{
    private static readonly OtpEunitManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpEunitNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpEunitNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpEunitNames.Application, Catalog.Resources[OtpEunitNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpEunitNames.Upgrade, Catalog.Resources[OtpEunitNames.Upgrade]);

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
            throw new InvalidDataException(OtpEunitNames.HashMismatch);

        return bytes;
    }

    private static OtpEunitManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpEunitNames.Catalog);

        return JsonSerializer.Deserialize<OtpEunitManifest>(input)
            ?? throw new InvalidDataException(OtpEunitNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpEunit).Assembly.GetManifestResourceStream(OtpEunitNames.Prefix + name)
            ?? throw new InvalidDataException(OtpEunitNames.MissingResource);
}
