using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpSyntaxTools
{
    private static readonly OtpSyntaxToolsManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpSyntaxToolsNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpSyntaxToolsNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpSyntaxToolsNames.Application, Catalog.Resources[OtpSyntaxToolsNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpSyntaxToolsNames.Upgrade, Catalog.Resources[OtpSyntaxToolsNames.Upgrade]);

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
            throw new InvalidDataException(OtpSyntaxToolsNames.HashMismatch);

        return bytes;
    }

    private static OtpSyntaxToolsManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpSyntaxToolsNames.Catalog);

        return JsonSerializer.Deserialize<OtpSyntaxToolsManifest>(input)
            ?? throw new InvalidDataException(OtpSyntaxToolsNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpSyntaxTools).Assembly.GetManifestResourceStream(OtpSyntaxToolsNames.Prefix + name)
            ?? throw new InvalidDataException(OtpSyntaxToolsNames.MissingResource);
}
