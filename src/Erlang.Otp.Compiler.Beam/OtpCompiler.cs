using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpCompiler
{
    private static readonly OtpCompilerManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpCompilerNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpCompilerNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpCompilerNames.Application, Catalog.Resources[OtpCompilerNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpCompilerNames.Upgrade, Catalog.Resources[OtpCompilerNames.Upgrade]);

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
            throw new InvalidDataException(OtpCompilerNames.HashMismatch);

        return bytes;
    }

    private static OtpCompilerManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpCompilerNames.Catalog);

        return JsonSerializer.Deserialize<OtpCompilerManifest>(input)
            ?? throw new InvalidDataException(OtpCompilerNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpCompiler).Assembly.GetManifestResourceStream(OtpCompilerNames.Prefix + name)
            ?? throw new InvalidDataException(OtpCompilerNames.MissingResource);
}
