using System.Reflection;
using System.Text.Json;

namespace Erlang.NifNativeBackend;

internal static class NifNativeDomainChecks
{
    private static NifNativeAllocationSnapshot OwnerSnapshot(NifNativeLibraryOwner owner)
        => ((NifNativeAllocationOwner)typeof(NifNativeLibraryOwner)
            .GetField(NifNativeDomainNames.AllocationOwnerField, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(owner)!).Snapshot;

    internal static void VetoClosed(ProcessRuntime runtime, NifNativeAllocationDomainSnapshot before)
    {
        // The reviewed fixture rejects a non-42 marker before calling enif_alloc.
        NifNativeAllocationDomainSnapshot after = runtime.NativeAllocationSnapshot;
        NifNativeHarness.Require(after == before && after.JoinedOwners == 0);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Role = NifNativeDomainNames.VetoRole,
            Snapshot = after
        }));
    }

    internal static NifNativeAllocationDomainSnapshot Loaded(ProcessRuntime runtime, NifNativeLibraryOwner owner)
    {
        NifNativeAllocationDomainSnapshot value = runtime.NativeAllocationSnapshot;
        NifNativeAllocationSnapshot actualOwner = OwnerSnapshot(owner);
        NifNativeHarness.Require(value.JoinedOwners == 1 && value.Allocations == actualOwner);
        NifNativeHarness.Require(actualOwner.Count == 1 && actualOwner.RequestedBytes > 0 && actualOwner.BackingRequestBytes >= actualOwner.RequestedBytes);
        NifNativeHarness.Require(actualOwner.SuccessfulAllocations == 1 && actualOwner.ReleasedAllocations == 0);
        NifNativeHarness.Require(actualOwner.AllocatedRequestedBytes == actualOwner.RequestedBytes && actualOwner.AllocatedBackingRequestBytes == actualOwner.BackingRequestBytes);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Role = NifNativeDomainNames.LoadedRole,
            Snapshot = value,
            OwnerSnapshot = actualOwner
        }));

        return value;
    }

    internal static void Retained(ProcessRuntime runtime, NifNativeAllocationDomainSnapshot loaded)
        => NifNativeHarness.Require(runtime.NativeAllocationSnapshot == loaded);

    internal static void Unloaded(ProcessRuntime runtime, NifNativeLibraryOwner owner, NifNativeAllocationDomainSnapshot loaded)
    {
        NifNativeAllocationDomainSnapshot value = runtime.NativeAllocationSnapshot;
        NifNativeAllocationSnapshot released = value.Allocations;
        NifNativeHarness.Require(value.JoinedOwners == 0 && released == OwnerSnapshot(owner));
        NifNativeHarness.Require(released.Count == 0 && released.RequestedBytes == 0 && released.BackingRequestBytes == 0);
        NifNativeHarness.Require(released.AllocatedRequestedBytes == loaded.Allocations.AllocatedRequestedBytes && released.AllocatedBackingRequestBytes == loaded.Allocations.AllocatedBackingRequestBytes);
        NifNativeHarness.Require(released.ReleasedRequestedBytes == released.AllocatedRequestedBytes && released.ReleasedBackingRequestBytes == released.AllocatedBackingRequestBytes);
        NifNativeHarness.Require(released.SuccessfulAllocations == loaded.Allocations.SuccessfulAllocations && released.ReleasedAllocations == released.SuccessfulAllocations);
        NifNativeHarness.Require(released.PeakRequestedBytes == loaded.Allocations.PeakRequestedBytes && released.PeakBackingRequestBytes == loaded.Allocations.PeakBackingRequestBytes);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Role = NifNativeDomainNames.UnloadedRole,
            Snapshot = value
        }));
    }
}
