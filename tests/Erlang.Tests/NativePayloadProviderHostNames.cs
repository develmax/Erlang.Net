using System.Reflection;

namespace Erlang.Tests;

internal static class NativePayloadProviderHostNames
{
    internal const string Test = "runtime/native-payload-provider-real-joined-request-extents";
    internal const string FactoryType = "Erlang.NativePayloadRequestProvider";
    internal const string Factory = "Create";
    internal const string Identity = "sdk_joined_nif_payload_requests";
    internal const string Requested = "requested_bytes";
    internal const string Backing = "backing_request_bytes";
    internal const string Instance = "instance";
    internal const string Capture = "CaptureAsync";
    internal const string NativeDomainField = "nativeAllocationDomain";
    internal const string Gate = "gate";
    internal const string Availability = "available";
    internal const string OwnerType = "Erlang.NifNativeAllocationOwner";
    internal const string Join = "JoinDomain";
    internal const string End = "TryEndDomainMembership";
    internal const string Producer = "producer";
    internal const string Known = "KnownAllocators";
    internal const string Measure = "MeasureCapture";
    internal const string Kill = "kill";
    internal const string Failure = "Actual joined native payload request provider assertion failed.";
    internal const string DifferentRuntime = "Native payload request provider belongs to another runtime.";
    internal const string Unknown = "sys_alloc";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
}
