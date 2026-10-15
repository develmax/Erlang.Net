using System.Reflection;

namespace Erlang.Tests;

internal static class NifAllocationDomainHostNames
{
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal const BindingFlags Statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    internal const string Test = "runtime/nif-runtime-native-payload-domain-lifecycle";
    internal const string LibraryType = "Erlang.NifNativeLibraryOwner";
    internal const string ProfileType = "Erlang.NifNativeCapabilityProfile";
    internal const string NamesType = "Erlang.NifNativeNames";
    internal const string DiagnosticsType = "Erlang.NifNativeDiagnostics";
    internal const string InitializerType = "Erlang.NifNativeImageInitializer";
    internal const string Initializer = "InitializeAsync";
    internal const string SourceHash = "SourceSha256";
    internal const string ModuleIdentity = "ModuleIdentity";
    internal const string Initialize = "Initialize";
    internal const string Allocations = "allocations";
    internal const string Gate = "gate";
    internal const string Disposed = "disposed";
    internal const string Domain = "NativeAllocationDomain";
    internal const string RuntimeSnapshot = "NativeAllocationSnapshot";
    internal const string Snapshot = "Snapshot";
    internal const string JoinedOwners = "JoinedOwners";
    internal const string Allocate = "Allocate";
    internal const string Free = "Free";
    internal const string Module = "nif_domain_host_wrong_module";
    internal const string Function = "fallback";
    internal const string NeverOpenedLibrary = "nif-domain-host-never-opened.dll";
    internal const string ReflectionContract = "Actual runtime native domain reflection contract is missing.";
    internal const string Broken = "Native payload domain lifecycle invariant failed.";
    internal const string EnrollmentMissing = "Real library payload exists but runtime enrollment or aggregate is missing.";
    internal const string UnexpectedInitialization = "Wrong-module native initialization was unexpectedly accepted.";
    internal const string ConstructedCancelledCase = "constructed-and-prejoin-cancelled-libraries-are-excluded";
    internal const string PayloadCase = "actual-library-enrollment-real-payload-zero-backing-and-disposal-history";
    internal const string AggregateCase = "two-library-simultaneous-global-peak-and-runtime-isolation";
    internal const string InitializerCase = "actual-reservation-initializer-failure-disposes-joined-membership";
    internal const string ConcurrentCase = "concurrent-library-backends-atomic-domain-snapshot-and-disposal-drain";
    internal const string FocusStatus = "ACTUAL_RUNTIME_DLL_NATIVE_PAYLOAD_DOMAIN_HOST_PASS";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
}
