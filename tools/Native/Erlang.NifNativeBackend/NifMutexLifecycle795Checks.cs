using System.Collections.Concurrent;
using System.Reflection;
namespace Erlang.NifNativeBackend;

internal static class NifMutexLifecycle795Checks
{
    internal static (NifNativeAllocationDomainSnapshot Domain, int Mutexes) Capture(ProcessRuntime runtime)
    {
        var registry = (ConcurrentDictionary<nint, NifNativeMutex795>)typeof(NifNativeAllocationOwner)
            .GetField(NifMutexLifecycle795Names.RegistryField, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        int actual = registry.Values.Count(mutex => ReferenceEquals(mutex.Owner.Arena528, runtime.NativeAllocationDomain));

        return (runtime.NativeAllocationSnapshot, actual);
    }

    internal static NifNativeLibraryOwner CaptureOwner(ModuleCodeImage image) =>
        (NifNativeLibraryOwner)typeof(NifImageBindings).GetField(NifMutexLifecycle795Names.NativeOwnerField,
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(image.NifBindings)!;

    internal static async Task AwaitActuallyRetiredImage(ModuleCodeImage image)
    {
        Require(image.NifBindings.Phase == NifImageBindingPhase.Retired);
        await image.NifBindings.RetireAsync().WaitAsync(NifNativeHarnessNames.Deadline);
    }

    internal static void FailedInitializationClosed(ProcessRuntime runtime,
        (NifNativeAllocationDomainSnapshot Domain, int Mutexes) before)
    {
        var after = Capture(runtime);
        Require(after.Mutexes == before.Mutexes);
        ReturnedLiveBaseline(before.Domain, after.Domain);
        Require(after.Domain.Allocations.SuccessfulAllocations > before.Domain.Allocations.SuccessfulAllocations);
    }

    internal static void SourceImageClosedWithLiveTransfer(ProcessRuntime runtime, NifNativeLibraryOwner oldOwner,
        (NifNativeAllocationDomainSnapshot Domain, int Mutexes) before)
    {
        var owner = Owner(oldOwner);
        Require(owner.PhysicalImageClosed528);
        Require((nint)typeof(NifNativeLibraryOwner).GetField(NifMutexLifecycle795Names.LibraryField,
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(oldOwner)! == 0);
        var current = Capture(runtime);
        Require(current.Mutexes == before.Mutexes + 1);
        Require(owner.Snapshot.Count == 2 && owner.Snapshot.RequestedBytes > 0);
    }

    internal static void ExplicitMutexDestroyed(ProcessRuntime runtime, NifNativeLibraryOwner oldOwner,
        (NifNativeAllocationDomainSnapshot Domain, int Mutexes) before)
    {
        Require(Capture(runtime).Mutexes == before.Mutexes);
        // The real transferred State is intentionally still live until B unload.
        Require(Owner(oldOwner).Snapshot.Count == 1);
    }

    internal static void BothImagesClosed(
        ProcessRuntime runtime,
        NifNativeLibraryOwner oldOwner,
        NifNativeLibraryOwner newOwner,
        (NifNativeAllocationDomainSnapshot Domain, int Mutexes) before
    )
    {
        Require(Owner(oldOwner).PhysicalImageClosed528 && Owner(newOwner).PhysicalImageClosed528);
        var after = Capture(runtime);
        Require(after.Mutexes == before.Mutexes);
        ReturnedLiveBaseline(before.Domain, after.Domain);
    }

    private static NifNativeAllocationOwner Owner(NifNativeLibraryOwner owner) =>
        (NifNativeAllocationOwner)typeof(NifNativeLibraryOwner).GetField(NifMutexLifecycle795Names.OwnerField,
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;

    private static void ReturnedLiveBaseline(NifNativeAllocationDomainSnapshot before, NifNativeAllocationDomainSnapshot after)
    {
        Require(after.JoinedOwners == before.JoinedOwners);
        var a = after.Allocations;
        var b = before.Allocations;
        Require(a.Count == b.Count && a.RequestedBytes == b.RequestedBytes && a.BackingRequestBytes == b.BackingRequestBytes);
        Require(a.SuccessfulAllocations - b.SuccessfulAllocations == a.ReleasedAllocations - b.ReleasedAllocations);
        Require(a.AllocatedRequestedBytes - b.AllocatedRequestedBytes == a.ReleasedRequestedBytes - b.ReleasedRequestedBytes);
        Require(a.AllocatedBackingRequestBytes - b.AllocatedBackingRequestBytes == a.ReleasedBackingRequestBytes - b.ReleasedBackingRequestBytes);
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(NifMutexLifecycle795Names.Failure);
    }
}
