using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Otp;

public static class OtpDebugger
{
    private static readonly OtpDebuggerManifest Catalog = LoadCatalog();

    public static IReadOnlyList<string> Modules { get; } = Array.AsReadOnly(Catalog.Images.Keys.Order(StringComparer.Ordinal).ToArray());

    public static IReadOnlyList<string> SupportFiles { get; } = Array.AsReadOnly(Catalog.Resources.Keys
        .Where(name => name.StartsWith(OtpDebuggerNames.SupportPrefix, StringComparison.Ordinal))
        .Select(name => name[OtpDebuggerNames.SupportPrefix.Length..]).Order(StringComparer.Ordinal).ToArray());

    public static byte[] ReadSupportFile(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string key = OtpDebuggerNames.SupportPrefix + name;
        if (!Catalog.Resources.TryGetValue(key, out string? hash))
            throw new ArgumentException(OtpDebuggerNames.UnknownSupportFile, nameof(name));

        return ReadVerified(key, hash);
    }

    public static byte[] ReadImage(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (!Catalog.Images.TryGetValue(module, out string? hash))
            throw new ArgumentException(OtpDebuggerNames.UnknownModule, nameof(module));

        return ReadVerified(module + OtpDebuggerNames.BeamExtension, hash);
    }

    public static byte[] ReadApplication()
        => ReadVerified(OtpDebuggerNames.Application, Catalog.Resources[OtpDebuggerNames.Application]);

    public static byte[] ReadApplicationUpgrade()
        => ReadVerified(OtpDebuggerNames.Upgrade, Catalog.Resources[OtpDebuggerNames.Upgrade]);

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
            throw new InvalidDataException(OtpDebuggerNames.HashMismatch);

        return bytes;
    }

    private static OtpDebuggerManifest LoadCatalog()
    {
        using Stream input = OpenResource(OtpDebuggerNames.Catalog);

        return JsonSerializer.Deserialize<OtpDebuggerManifest>(input)
            ?? throw new InvalidDataException(OtpDebuggerNames.MissingResource);
    }

    private static Stream OpenResource(string name)
        => typeof(OtpDebugger).Assembly.GetManifestResourceStream(OtpDebuggerNames.Prefix + name)
            ?? throw new InvalidDataException(OtpDebuggerNames.MissingResource);
}
