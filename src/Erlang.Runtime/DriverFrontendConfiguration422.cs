using System.Collections.Frozen;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang;

internal sealed class DriverFrontendConfiguration422
{
    internal DriverFrontendConfiguration422(
        ErlDriverHostPackage420 package,
        ErlDriverSystemInfo422 systemInfo,
        string startupDirectory,
        IReadOnlyDictionary<int, string> admittedErrnoNames,
        ErlDriverHostLaunchOptions420 launch
    )
    {
        Package = package;
        SystemInfo = systemInfo;
        Launch = launch;
        if (!Path.IsPathFullyQualified(startupDirectory)) throw new ArgumentException(DriverParentWireNames422.Invalid, nameof(startupDirectory));
        StartupDirectory = Path.GetFullPath(startupDirectory);
        ErrnoNames = admittedErrnoNames.ToFrozenDictionary();
    }

    internal ErlDriverHostPackage420 Package { get; }
    internal ErlDriverSystemInfo422 SystemInfo { get; }
    internal ErlDriverHostLaunchOptions420 Launch { get; }
    internal string StartupDirectory { get; }
    internal IReadOnlyDictionary<int, string> ErrnoNames { get; }

    internal string Errno(int nativeError) => ErrnoNames.TryGetValue(nativeError, out string? value)
        ? value : ErlDriverPrimitiveNames422.ErrnoPrefix + nativeError.ToString(CultureInfo.InvariantCulture);

    internal static IReadOnlyDictionary<int, string> ReadErrnoManifest(string path, string expectedSha256)
    {
        byte[] bytes = File.ReadAllBytes(path);
        if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(expectedSha256, StringComparison.Ordinal))
            throw new InvalidDataException(DriverParentWireNames422.Invalid);
        var values = JsonSerializer.Deserialize<Dictionary<int, string>>(bytes)
            ?? throw new InvalidDataException(DriverParentWireNames422.Invalid);

        return values.ToFrozenDictionary();
    }
}
