using System.Runtime.InteropServices;

namespace Erlang.NifNativeBackend;

internal static partial class NifNativeReallocationChecks
{
    private static async Task MixedProfile(NifNativeCapabilityProfile legacy)
    {
        await WithLibrary(
            legacy,
            async (
                runtime,
                context,
                image,
                library
            ) =>
        {
            Require(!library.Granted);
            using (var call = image.NifBindings.Admit(NifNativeNames.Increment, NifNativeNames.IncrementArity)!)
                Require((await call.InvokeAsync(context, [Term.I(NifNativeReallocationParameters.IncrementInput)])).Equals(Term.I(NifNativeReallocationParameters.IncrementOutput)));
            using (var greeting = image.NifBindings.Admit(NifNativeNames.Greeting, NifNativeNames.GreetingArity)!)
                Require((await greeting.InvokeAsync(context, [])).Equals(Term.String(NifNativeHarnessNames.Greeting)));
            lock (library.Gate)
            {
                var beforeOwner = library.Allocations.Snapshot;
                var beforeDomain = runtime.NativeAllocationSnapshot;
                Denied(library, OperatingSystem.IsLinux());
                Require(library.Allocations.Snapshot == beforeOwner && runtime.NativeAllocationSnapshot == beforeDomain);
                Nested(library);
                Require(library.Allocations.Snapshot.Count == beforeOwner.Count);
                Require(library.Allocations.Snapshot.RequestedBytes == beforeOwner.RequestedBytes);
                Require(library.Allocations.Snapshot.BackingRequestBytes == beforeOwner.BackingRequestBytes);
            }
        }
        );
    }

    private static unsafe nint Call(bool resident, nint pointer, nuint size)
    {
        delegate* unmanaged[Cdecl]<nint, nuint, nint> function = resident
            ? (delegate* unmanaged[Cdecl]<nint, nuint, nint>)NifNativePlatformLoader.Symbol(0, NifNativeReallocationNames.Reallocate)
            : &NifNativePrimitiveCallbacks.Reallocate;

        return function(pointer, size);
    }

    private static void Denied(NifNativeReallocationLibrary library, bool resident)
    {
        var failure = new NifNativeFailureState();
        using var oldScope = new NifNativeCallbackScope(library.Allocations, failure);
        Require(!NifNativeCallbackScope.Current.ReallocationEnabled);
        Require(Call(resident, 0, (nuint)NifNativeReallocationParameters.InitialBytes) == 0);
        Require(failure.HostFailure?.SourceException is InvalidOperationException rejected && rejected.Message == NifNativeDiagnostics.ReallocationNotGranted);
        Require(!failure.HasGuestException);
    }

    private static void Nested(NifNativeReallocationLibrary library)
    {
        var outerFailure = new NifNativeFailureState();
        using (var oldOuter = new NifNativeCallbackScope(library.Allocations, outerFailure))
        {
            using (var newInner = new NifNativeCallbackScope(library.Allocations, outerFailure, true))
            {
                Require(NifNativeCallbackScope.Current.ReallocationEnabled);
                SmallGranted(library);
            }
            Require(!NifNativeCallbackScope.Current.ReallocationEnabled);
        }
        var grantedFailure = new NifNativeFailureState();
        using var newOuter = new NifNativeCallbackScope(library.Allocations, grantedFailure, true);
        Require(NifNativeCallbackScope.Current.ReallocationEnabled);
        Denied(library, false);
        Require(NifNativeCallbackScope.Current.ReallocationEnabled);
        SmallGranted(library);
        grantedFailure.ThrowPending();
        outerFailure.ThrowPending();
    }

    private static void SmallGranted(NifNativeReallocationLibrary library)
    {
        nint pointer = Call(false, 0, (nuint)NifNativeReallocationParameters.InitialBytes);
        Require(pointer != 0);
        try
        {
            Marshal.WriteByte(pointer, NifNativeReallocationParameters.DataSentinel);
            nint resized = Call(false, pointer, (nuint)NifNativeReallocationParameters.GrownBytes);
            Require(resized != 0);
            pointer = resized;
            Require(Marshal.ReadByte(pointer) == NifNativeReallocationParameters.DataSentinel);
        }
        finally
        {
            library.Allocations.Free(pointer);
        }
    }
}
