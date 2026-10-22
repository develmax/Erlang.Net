namespace Erlang;

public sealed class RuntimeDriverFrontendOptions
{
    public RuntimeDriverFrontendOptions(
        string packageManifest,
        string packageSha256,
        int asyncThreads = RuntimeDriverFrontendOptionNames424.DefaultAsyncThreads,
        TimeSpan? startupTimeout = null,
        TimeSpan? shutdownTimeout = null,
        int maximumFrameBytes = RuntimeDriverFrontendOptionNames424.DefaultFrameBytes
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageManifest);
        ArgumentNullException.ThrowIfNull(packageSha256);
        if (packageSha256.Length != 64 || !packageSha256.All(value => value is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F'))
            throw new ArgumentException(RuntimeDriverFrontendOptionNames424.InvalidHash, nameof(packageSha256));
        if (asyncThreads < 0 || asyncThreads > RuntimeDriverFrontendOptionNames424.MaximumAsyncThreads)
            throw new ArgumentOutOfRangeException(nameof(asyncThreads));
        TimeSpan startup = startupTimeout ?? RuntimeDriverFrontendOptionNames424.DefaultStartupTimeout;
        TimeSpan shutdown = shutdownTimeout ?? RuntimeDriverFrontendOptionNames424.DefaultShutdownTimeout;
        if (startup <= TimeSpan.Zero || startup.TotalMilliseconds > uint.MaxValue - 1) throw new ArgumentOutOfRangeException(nameof(startupTimeout));
        if (shutdown <= TimeSpan.Zero || shutdown.TotalMilliseconds > uint.MaxValue - 1) throw new ArgumentOutOfRangeException(nameof(shutdownTimeout));
        if (maximumFrameBytes < ErlDriverHostNames420.MinimumFrameBytes) throw new ArgumentOutOfRangeException(nameof(maximumFrameBytes));
        PackageManifest = Path.GetFullPath(packageManifest);
        PackageSha256 = packageSha256.ToUpperInvariant();
        AsyncThreads = asyncThreads;
        StartupTimeout = startup;
        ShutdownTimeout = shutdown;
        MaximumFrameBytes = maximumFrameBytes;
    }

    public string PackageManifest { get; }
    public string PackageSha256 { get; }
    public int AsyncThreads { get; }
    public TimeSpan StartupTimeout { get; }
    public TimeSpan ShutdownTimeout { get; }
    public int MaximumFrameBytes { get; }

    internal DriverFrontendConfiguration422 Bind(ProcessRuntime runtime, CancellationToken cancellation)
        => RuntimeDriverPackageAdmission424.Bind(this, runtime, cancellation);
}
