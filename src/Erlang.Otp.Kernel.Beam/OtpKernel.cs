using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpKernel
{
    private static readonly OtpKernelManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpKernelNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpKernelNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpKernelNames.Application, Catalog.Resources[OtpKernelNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpKernelNames.Upgrade, Catalog.Resources[OtpKernelNames.Upgrade]);

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
            throw new InvalidDataException(OtpKernelNames.HashMismatch);

        return bytes;
    }

    private static OtpKernelManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpKernelNames.Catalog);

        return JsonSerializer.Deserialize<OtpKernelManifest>(input)
            ?? throw new InvalidDataException(OtpKernelNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpKernel).Assembly.GetManifestResourceStream(OtpKernelNames.Prefix + name)
            ?? throw new InvalidDataException(OtpKernelNames.MissingResource);
}
