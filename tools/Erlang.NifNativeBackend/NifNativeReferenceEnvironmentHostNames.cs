namespace Erlang.NifNativeBackend;

internal static class NifNativeReferenceEnvironmentHostNames
{
    internal const string Module = "reference_environment_host_contract";
    internal const string Failure = "Independent reference environment contract failed";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
