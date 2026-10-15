using System.Runtime.InteropServices;

namespace Erlang.NifNativeBackend;

internal static class NifNativeManagedChecks
{
    internal static unsafe Task Run()
    {
        NifNativeHarness.Require(sizeof(NifNativeBridgeTable) == NifNativeAbi.BridgeTableBytes);
        var metadata = new ModuleRegistry();
        metadata.AddNifTaint(NifNativeNames.Module);
        metadata.AddNifTaint(NifNativeHarnessNames.TaintSecond);
        metadata.AddNifTaint(NifNativeNames.Module);
        string[] snapshot = metadata.GetNifTaints();
        NifNativeHarness.Require(snapshot.SequenceEqual(new[] { NifNativeNames.Module, NifNativeHarnessNames.TaintSecond }));
        snapshot[0] = NifNativeHarnessNames.TaintSecond;
        NifNativeHarness.Require(metadata.GetNifTaints()[0] == NifNativeNames.Module);
        var latest = new NifNativeFailureState();
        latest.Raise(NifNativeHarnessNames.FirstReason);
        latest.Raise(NifNativeHarnessNames.LastReason);
        NifNativeHarness.Guest(latest.ThrowPending, NifNativeHarnessNames.LastReason);
        var host = new InvalidOperationException(NifNativeHarnessNames.HostError);
        latest.FailHost(host);
        try
        {
            latest.ThrowPending();
            throw new InvalidOperationException(NifNativeHarnessNames.Broken);
        }
        catch (NifNativeHostFailureException observed) when (ReferenceEquals(observed.HostFailure.SourceException, host))
        {
            NifNativeHarness.Require(observed.GuestPendingReason.Equals(NifNativeHarnessNames.LastReason));
        }
        nint shortEntry = Marshal.AllocHGlobal(NifNativeAbi.EntryVersionPrefixBytes);
        try
        {
            Marshal.WriteInt32(shortEntry, NifNativeAbi.MajorVersion);
            Marshal.WriteInt32(shortEntry, sizeof(int), NifNativeAbi.MinorVersion - 1);
            try
            {
                NifNativeEntryReader.Read(shortEntry);
                throw new InvalidOperationException(NifNativeHarnessNames.Broken);
            }
            catch (NotSupportedException)
            {
            }
        }
        finally
        {
            Marshal.FreeHGlobal(shortEntry);
        }

        return EnvironmentChecks();
    }

    private static async Task EnvironmentChecks()
    {
        await using var runtime = new ProcessRuntime();
        await NifNativeHarness.Context(runtime, context =>
        {
            Callbacks(context);

            return Task.CompletedTask;
        });
    }

    private static unsafe void Callbacks(ProcessContext context)
    {
        int before = NifNativeCallEnvironment.ActiveCount;
        var allocations = new NifNativeAllocationOwner();
        var failure = new NifNativeFailureState();
        var image = new ModuleCodeImage(NifNativeNames.Module);
        nint expired;
        using (var scope = new NifNativeCallbackScope(allocations, failure))
        {
            using (var environment = new NifNativeCallEnvironment(
                context,
                image,
                0,
                failure
            ))
            {
                expired = environment.Pointer;
                delegate* unmanaged[Cdecl]<nint, nuint*, int> pending = &NifNativePrimitiveCallbacks.HasPendingException;
                delegate* unmanaged[Cdecl]<nint, nuint, nuint> raise = &NifNativePrimitiveCallbacks.RaiseException;
                delegate* unmanaged[Cdecl]<nint, nuint, int> isException = &NifNativePrimitiveCallbacks.IsException;
                delegate* unmanaged[Cdecl]<nint, int, nuint> makeInt = &NifNativePrimitiveCallbacks.MakeInt;
                nuint untouched = 123;
                NifNativeHarness.Require(pending(environment.Pointer, &untouched) == 0 && untouched == 123);
                nuint first = environment.Add(NifNativeHarnessNames.FirstReason);
                nuint last = environment.Add(NifNativeHarnessNames.LastReason);
                NifNativeHarness.Require(raise(environment.Pointer, first) == 0);
                NifNativeHarness.Require(raise(environment.Pointer, last) == 0);
                nuint ordinary = makeInt(environment.Pointer, 42);
                NifNativeHarness.Require(isException(environment.Pointer, ordinary) == 0);
                NifNativeHarness.Require(isException(environment.Pointer, 0) == 1);
                NifNativeHarness.Require(pending(environment.Pointer, null) == 1);
                nuint reason;
                NifNativeHarness.Require(pending(environment.Pointer, &reason) == 1 && reason == last && environment.Get(reason).Equals(NifNativeHarnessNames.LastReason));
                NifNativeHarness.Guest(failure.ThrowPending, NifNativeHarnessNames.LastReason);
                var separate = new NifNativeFailureState();
                using (var nested = new NifNativeCallbackScope(allocations, separate))
                {
                    NifNativeHarness.Require(raise(environment.Pointer, nuint.MaxValue) == 0);
                    NifNativeHarness.Require(separate.HostFailure is not null);
                    NifNativeHarness.Require(failure.GuestPendingReason!.Equals(NifNativeHarnessNames.LastReason));
                }
            }
            delegate* unmanaged[Cdecl]<nint, nint> privateData = &NifNativePrimitiveCallbacks.PrivateData;
            NifNativeHarness.Require(privateData(expired) == 0 && failure.HostFailure is not null);
        }
        NifNativeHarness.Require(NifNativeCallEnvironment.ActiveCount == before && allocations.Count == 0);
    }
}
