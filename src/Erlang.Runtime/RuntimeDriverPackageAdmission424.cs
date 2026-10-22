using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Erlang;

internal static class RuntimeDriverPackageAdmission424
{
    private static readonly JsonSerializerOptions Json = new() { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };

    internal static DriverFrontendConfiguration422 Bind(RuntimeDriverFrontendOptions options, ProcessRuntime runtime, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
            throw new PlatformNotSupportedException(RuntimeDriverFrontendOptionNames424.UnsupportedProfile);
        byte[] manifestBytes = File.ReadAllBytes(options.PackageManifest);
        if (Convert.ToHexString(SHA256.HashData(manifestBytes)) != options.PackageSha256)
            throw new InvalidDataException(RuntimeDriverFrontendOptionNames424.InvalidPackage);
        var package = ErlDriverHostPackage420.Read(options.PackageManifest, options.PackageSha256);
        var manifest = JsonSerializer.Deserialize<ErlDriverHostPackageManifest420>(manifestBytes, Json)
            ?? throw new InvalidDataException(RuntimeDriverFrontendOptionNames424.InvalidPackage);
        string root = Path.GetDirectoryName(options.PackageManifest)!;
        string childRoot = Path.GetDirectoryName(package.EntryPoint)!;

        string Resolve(ErlDriverHostPackageFile420 file) => Path.GetFullPath(Path.Combine(root, file.Path));

        byte[] Included(string full, string? expected = null)
        {
            cancellation.ThrowIfCancellationRequested();
            var rows = manifest.Files.Where(row => Resolve(row).Equals(full, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (rows.Length != 1 || expected is not null && rows[0].Sha256 != expected)
                throw new InvalidDataException(RuntimeDriverFrontendOptionNames424.InvalidPackage);
            byte[] bytes = File.ReadAllBytes(full);
            if (Convert.ToHexString(SHA256.HashData(bytes)) != rows[0].Sha256)
                throw new InvalidDataException(RuntimeDriverFrontendOptionNames424.InvalidPackage);

            return bytes;
        }

        string RuntimeHash(Assembly assembly)
        {
            string path = assembly.Location;
            if (string.IsNullOrEmpty(path)) throw new PlatformNotSupportedException(RuntimeDriverFrontendOptionNames424.UnavailableAssembly);
            string digest;
            using (var stream = File.OpenRead(path)) digest = Convert.ToHexString(SHA256.HashData(stream));
            var rows = manifest.Files.Where(row => Path.GetFileName(row.Path).Equals(Path.GetFileName(path), StringComparison.OrdinalIgnoreCase)).ToArray();
            if (rows.Length != 1 || rows[0].Sha256 != digest)
                throw new InvalidDataException(RuntimeDriverFrontendOptionNames424.RuntimeMismatch);
            Included(Resolve(rows[0]), digest);

            return digest;
        }
        string runtimeHash = RuntimeHash(typeof(ProcessRuntime).Assembly);
        foreach (Assembly assembly in new[] { typeof(Term).Assembly, typeof(ExternalTermFormat).Assembly }.Distinct()) RuntimeHash(assembly);
        byte[] configurationBytes = Included(Path.Combine(childRoot, ErlDriverHostNames420.ConfigurationFile));
        var configuration = JsonSerializer.Deserialize<ErlDriverServerConfiguration420>(configurationBytes, Json)
            ?? throw new InvalidDataException(RuntimeDriverFrontendOptionNames424.InvalidPackage);

        string ChildFile(string relative, string hash)
        {
            if (Path.IsPathRooted(relative)) throw new InvalidDataException(RuntimeDriverFrontendOptionNames424.InvalidPackage);
            string full = Path.GetFullPath(Path.Combine(childRoot, relative));
            if (!full.StartsWith(Path.TrimEndingDirectorySeparator(childRoot) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(RuntimeDriverFrontendOptionNames424.InvalidPackage);
            Included(full, hash);

            return full;
        }
        ChildFile(configuration.AbiFile, configuration.AbiSha256);
        ChildFile(configuration.BridgeFile, configuration.BridgeSha256);
        string errno = ChildFile(configuration.ErrnoFile, configuration.ErrnoSha256);
        var mapping = DriverFrontendConfiguration422.ReadErrnoManifest(errno, configuration.ErrnoSha256);
        cancellation.ThrowIfCancellationRequested();
        // The child constructs exactly this many native async worker threads.
        // Normal lanes are this actual fresh runtime's capacity; NIF versions
        // reuse the actual bridge ABI constants. Dirty native work has genuine
        // CPU/IO worker support in this backend, not an ERTS/JIT identity claim.
        var info = new ErlDriverSystemInfo422(
            RuntimeDriverFrontendOptionNames424.RuntimePrefix + runtimeHash,
            RuntimeDriverFrontendOptionNames424.OtpContractRelease,
            options.AsyncThreads,
            runtime.ActorDispatchCapacity,
            NifNativeAbi.MajorVersion,
            NifNativeAbi.MinorVersion,
            true
        );

        return new(
            package,
            info,
            Environment.CurrentDirectory,
            mapping,
            new(options.StartupTimeout, options.ShutdownTimeout, options.MaximumFrameBytes)
        );
    }
}
