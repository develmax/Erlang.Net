using System.Reflection;

namespace Erlang.Tests;

internal static class IdentifierLifetimeHostNames
{
    internal const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal const string KnownNodes = "KnownNodes";
    internal const string Observed = "observed";
    internal const string Observe = "Observe";
    internal const string TryGetValue = "TryGetValue";
    internal const string Identities = "distributionEntryIdentities";
    internal const string Current = "currentDistributionEntry";
    internal const string IdentityType = "Erlang.RuntimeDistributionEntryIdentity";
    internal const string Kernel = "net_kernel";
    internal const string Local = "lifetime403@local";
    internal const string Remote = "lifetime403@remote";
    internal const string ChurnSuffix = "@remote";
    internal const int FunctionUniqueBytes = 16;
    internal const int PendingCapacity = 1;
    internal const string ChurnPrefix = "lifetime403_";
    internal const string Module = "identifier_lifetime403";
    internal const string Failure = "External identifier DistEntry lifetime assertion failed.";
    internal const string PidTest = "host_identifier403_imported_pid_lifetime";
    internal const string PortTest = "host_identifier403_imported_port_lifetime";
    internal const string ReferenceTest = "host_identifier403_imported_reference_lifetime";
    internal const string FunctionTest = "host_identifier403_nested_fun_environment_lifetime";
    internal const string DomainTest = "host_identifier403_local_foreign_closed_roots";
    internal const string ConcurrentTest = "host_identifier403_concurrent_identity_and_import_churn";
    internal const string Creation = "Creation";
    internal const string Pid = "pid";
    internal const string Port = "port";
    internal const string Reference = "reference";
    internal const string Function = "function";
    internal const string NativeModule = "erts_internal";
    internal const string Getter = "get_creation";
    internal const string ErlangModule = "erlang";
    internal const string SetNode = "setnode";
    internal const uint LocalCreation = 37;
    internal const uint RemoteCreation = 1;
    internal const int MaximumProcesses = 8;
    internal const int ChurnFirst = 256;
    internal const int ChurnSecond = 65;
    internal const int ChurnBound = 128;
    internal const int ConcurrentCount = 32;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
