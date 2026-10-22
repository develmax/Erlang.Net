namespace Erlang;

internal static class RuntimeDriverFrontendOptionNames424
{
    internal const string UnsupportedProfile = "Configured native drivers require the reviewed Windows x64 backend";
    internal const string InvalidHash = "Trusted driver package requires an explicit SHA256 digest";
    internal const string RuntimeMismatch = "Driver host package must contain exactly the current runtime, terms and serializer assemblies";
    internal const string InvalidPackage = "Driver host configuration and its dependencies must be included in the authenticated package";
    internal const string UnavailableAssembly = "Driver package admission requires physical managed assembly files";
    internal const string RuntimePrefix = "Erlang.Net/runtime-sha256-";
    internal const string OtpContractRelease = "29";
    internal const int DefaultAsyncThreads = 1;
    internal const int MaximumAsyncThreads = 1024;
    internal const int DefaultFrameBytes = 16 * 1024 * 1024;
    internal static readonly TimeSpan DefaultStartupTimeout = TimeSpan.FromSeconds(15);
    internal static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(15);
}
