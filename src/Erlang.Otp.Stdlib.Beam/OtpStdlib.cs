using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpStdlib
{
    private static readonly IReadOnlyDictionary<string, OtpStdlibImage> Catalog = LoadCatalog();
    private static readonly IReadOnlyDictionary<string, string> ResourceHashes = LoadResourceHashes();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Keys.Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadImage(string module)
    {
        OtpStdlibImage entry = RequireModule(module);

        return ReadVerified(module + OtpStdlibNames.BeamExtension, entry.Sha256);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpStdlibNames.Application, ResourceHashes[OtpStdlibNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpStdlibNames.Upgrade, ResourceHashes[OtpStdlibNames.Upgrade]);

    private static byte[] ReadVerified(string name, string hash)
    {
        using Stream input = OpenResource(name);
        using var output = new MemoryStream();
        input.CopyTo(output);
        byte[] bytes = output.ToArray();
        if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(hash, StringComparison.Ordinal))
            throw new InvalidDataException(OtpStdlibNames.HashMismatch);

        return bytes;
    }

    public static RuntimeModuleSelection Select(
        string module,
        RuntimeModuleBackend backend = RuntimeModuleBackend.Beam,
        IEnumerable<string>? dependencies = null
    )
    {
        OtpStdlibImage entry = RequireModule(module);
        if (backend == RuntimeModuleBackend.Managed)
        {
            if (dependencies is not null && dependencies.Any())
                throw new ArgumentException(OtpStdlibNames.ExplicitDependencies, nameof(dependencies));

            return RuntimeModuleSelection.Managed(module);
        }
        if (backend != RuntimeModuleBackend.Beam)
            throw new ArgumentOutOfRangeException(nameof(backend));
        if (dependencies is null && entry.RequiresExplicitDependencies)
            throw new ArgumentException(OtpStdlibNames.ExplicitDependencies, nameof(dependencies));

        return RuntimeModuleSelection.Beam(module, ReadImage(module), dependencies ?? entry.Dependencies);
    }

    private static OtpStdlibImage RequireModule(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.TryGetValue(module, out OtpStdlibImage? entry))
            throw new ArgumentException(OtpStdlibNames.UnknownModule, nameof(module));

        return entry;
    }

    private static IReadOnlyDictionary<string, OtpStdlibImage> LoadCatalog()
    {
        using Stream stream = OpenResource(OtpStdlibNames.Catalog);
        OtpStdlibImage[] entries = JsonSerializer.Deserialize<OtpStdlibImage[]>(stream)
            ?? throw new InvalidDataException(OtpStdlibNames.MissingResource);

        return entries.ToDictionary(entry => entry.Name, StringComparer.Ordinal);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpStdlib).Assembly.GetManifestResourceStream(OtpStdlibNames.ResourcePrefix + name)
            ?? throw new InvalidDataException(OtpStdlibNames.MissingResource);

    private static IReadOnlyDictionary<string, string> LoadResourceHashes()
    {
        using Stream input = OpenResource(OtpStdlibNames.Resources);

        return JsonSerializer.Deserialize<Dictionary<string, string>>(input)
            ?? throw new InvalidDataException(OtpStdlibNames.MissingResource);
    }
}
