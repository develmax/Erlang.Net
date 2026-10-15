using System.Text.Json;

namespace Erlang.NifNativeBackend;

internal static partial class NifNativeReallocationChecks
{
    private static async Task NativeCase(NifNativeCapabilityProfile profile, NifNativeReallocationCase item)
    {
        ProcessRuntime? actualRuntime = null;
        NifNativeReallocationLibrary? actualLibrary = null;
        NifNativeAllocationSnapshot afterCall = default;
        Term? returned = null;
        int actualMask = 0;
        byte[]? returnedEtf = null;
        await WithLibrary(
            profile,
            async (
                runtime,
                context,
                image,
                library
            ) =>
        {
            actualRuntime = runtime;
            actualLibrary = library;
            Require(library.Granted && library.Allocations.Snapshot == default);
            Require(runtime.NativeAllocationSnapshot.JoinedOwners == 1);
            using (var greeting = image.NifBindings.Admit(NifNativeNames.Greeting, NifNativeNames.GreetingArity)!)
                Require((await greeting.InvokeAsync(context, [])).Equals(Term.String(NifNativeReallocationHarnessNames.Greeting)));
            using (var call = image.NifBindings.Admit(NifNativeNames.Increment, NifNativeNames.IncrementArity)!)
                returned = await call.InvokeAsync(context, [Term.I(item.Selector)]);
            Require(returned.Equals(Term.I(item.ExpectedMask)));
            actualMask = checked((int)((Integer)returned).Value);
            returnedEtf = ((BitString)await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ErlangEtfNames.Encode,
                returned
            )).ToArray();
            afterCall = library.Allocations.Snapshot;
            ulong requestedHistory = checked((ulong)item.RequestedSizes.Sum());
            ulong backingHistory = checked((ulong)item.RequestedSizes.Sum(size => Math.Max(size, 1)));
            Require(afterCall.Count == 0 && afterCall.RequestedBytes == 0 && afterCall.BackingRequestBytes == 0);
            Require(afterCall.AllocatedRequestedBytes == requestedHistory && afterCall.ReleasedRequestedBytes == requestedHistory);
            Require(afterCall.AllocatedBackingRequestBytes == backingHistory && afterCall.ReleasedBackingRequestBytes == backingHistory);
            Require(afterCall.PeakRequestedBytes == (ulong)item.RequestedSizes.Max());
            Require(afterCall.PeakBackingRequestBytes == (ulong)Math.Max(item.RequestedSizes.Max(), 1));
            Require(afterCall.SuccessfulAllocations == 1 && afterCall.ReleasedAllocations == 1);
            Require(runtime.NativeAllocationSnapshot.Allocations == afterCall);
            using var heldAdmission = image.NifBindings.Admit(NifNativeNames.Greeting, NifNativeNames.GreetingArity)!;
            Task retirement = runtime.Modules.RetireNifImageAsync(image);
            Require(!retirement.IsCompleted && runtime.NativeAllocationSnapshot.JoinedOwners == 1);
            heldAdmission.Dispose();
            await retirement.WaitAsync(NifNativeReallocationParameters.Deadline);
            Require(runtime.NativeAllocationSnapshot.JoinedOwners == 0);

        }
        );
        Require(actualLibrary!.Owner.OutstandingAfterNativeUnload == 0);
        Require(actualLibrary.Allocations.Snapshot == afterCall);
        Require(actualRuntime!.NativeAllocationSnapshot.Allocations == afterCall);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Role = NifNativeReallocationHarnessNames.CaseRole,
            Case = item.Name,
            Selector = item.Selector,
            ReturnedMask = actualMask,
            ReturnedEtfBase64 = Convert.ToBase64String(returnedEtf!),
            RequestedSizes = item.RequestedSizes,
            RequestedSizesKind = NifNativeReallocationHarnessNames.SizesKind,
            HeldAdmissionDelayedUnload = true,
            OwnerSnapshot = afterCall,
            UnloadedDomainSnapshot = actualRuntime.NativeAllocationSnapshot
        }));
    }

    private static async Task OneShot(NifNativeCapabilityProfile profile, NifNativeCapabilityProfile legacy)
    {
        var oldConstructor = typeof(NifNativeCapabilityProfile).GetConstructors(NifNativeReallocationHarnessNames.Constructors)
            .Single(constructor => constructor.GetParameters().Length == NifNativeReallocationParameters.LegacyProfileParameterCount);
        var reflectedLegacy = (NifNativeCapabilityProfile)oldConstructor.Invoke([
            legacy.LibraryPath, legacy.LibrarySha256, NifNativeNames.SourceSha256, legacy.BridgePath, legacy.BridgeSha256
        ]);
        Require(!reflectedLegacy.ReallocationEnabled);
        await using var runtime = new ProcessRuntime();
        var source = Register(runtime);
        await NifNativeHarness.Context(
            runtime,
            async context =>
        {
            foreach (bool startNew in new[] { false, true })
            {
                await using var owner = new NifNativeLibraryOwner(new ModuleCodeImage(NifNativeReallocationHarnessNames.WrongModule), context);
                var library = new NifNativeReallocationLibrary(owner);
                Require(runtime.NativeAllocationSnapshot.JoinedOwners == 0);
                try
                {
                    owner.Initialize(
                        startNew ? profile : reflectedLegacy,
                        source.Descriptor,
                        Nil.Value,
                        CancellationToken.None
                    );
                    throw new InvalidOperationException(NifNativeReallocationHarnessNames.Broken);
                }
                catch (NotSupportedException failure) when (failure.Message == NifNativeDiagnostics.ModuleIdentity) { }
                Require(library.Granted == startNew && runtime.NativeAllocationSnapshot.JoinedOwners == 1);
                var failedFirst = runtime.NativeAllocationSnapshot;
                try
                {
                    owner.Initialize(
                        startNew ? reflectedLegacy : profile,
                        source.Descriptor,
                        Nil.Value,
                        CancellationToken.None
                    );
                    throw new InvalidOperationException(NifNativeReallocationHarnessNames.Broken);
                }
                catch (InvalidOperationException failure) when (failure.Message == NifNativeDiagnostics.RepeatedInitialization) { }
                Require(library.Granted == startNew && runtime.NativeAllocationSnapshot == failedFirst);
                await owner.DisposeAsync();
                var closed = runtime.NativeAllocationSnapshot;
                Require(closed.JoinedOwners == 0);
                try
                {
                    owner.Initialize(
                        profile,
                        source.Descriptor,
                        Nil.Value,
                        CancellationToken.None
                    );
                    throw new InvalidOperationException(NifNativeReallocationHarnessNames.Broken);
                }
                catch (ObjectDisposedException) { }
                Require(runtime.NativeAllocationSnapshot == closed && library.Granted == startNew);
            }
            await using var neverStarted = new NifNativeLibraryOwner(new ModuleCodeImage(NifNativeReallocationHarnessNames.WrongModule), context);
            await neverStarted.DisposeAsync();
            var before = runtime.NativeAllocationSnapshot;
            try
            {
                neverStarted.Initialize(
                    profile,
                    source.Descriptor,
                    Nil.Value,
                    CancellationToken.None
                );
                throw new InvalidOperationException(NifNativeReallocationHarnessNames.Broken);
            }
            catch (ObjectDisposedException) { }
            Require(runtime.NativeAllocationSnapshot == before);
        }
        );
    }
}
