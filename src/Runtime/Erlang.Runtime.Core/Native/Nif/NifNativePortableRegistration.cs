using System.Runtime.InteropServices;

namespace Erlang;

// OTP has one VM per process. Without dlmopen, retain that ownership model
// explicitly rather than sharing a VM-bound enif_alloc_env callback accidentally.
internal static unsafe class NifNativePortableRegistration
{
    private static readonly object Gate = new();
    private static ProcessRuntime? owner;
    private static NifNativeBridgeTable* table;
    private static nint library;
    private static NifNativeLinuxMutex795? mutexes;
    private static System.Runtime.ExceptionServices.ExceptionDispatchInfo? initializationFailure;

    internal static void EnsureRegistered(ProcessRuntime runtime, NifNativeCapabilityProfile profile)
    {
        NifNativeLifecycleGate.RequireDefaultProvider();
        if (profile.ExpectedLibrarySha256 is not null)
            throw new PlatformNotSupportedException(NifNativePortableNames.ReviewedLoading);
        if (profile.ReallocationEnabled)
            throw new PlatformNotSupportedException(NifNativePortableNames.UnsupportedReallocation);
        lock (Gate)
        {
            nint bridge = NifNativePortableUnixApi.EnsureLoaded(profile.BridgePath);
            NifNativePortableLayout.Verify(bridge);
            if (owner is not null)
            {
                if (!ReferenceEquals(owner, runtime))
                    throw new InvalidOperationException(NifNativePortableNames.SingleRuntime);
                initializationFailure?.Throw();

                return;
            }
            // A foreign VM/bridge exporting the same ABI cannot own our terms.
            foreach (string name in NifNativeNames.BridgeSymbols)
            {
                nint existing = NifNativePortableUnixApi.Symbol(0, name);
                if (existing != 0 && existing != NativeLibrary.GetExport(bridge, name))
                    throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
            }
            string path = profile.BridgePath ?? Path.Combine(AppContext.BaseDirectory, NifNativePortableNames.DefaultBridge);
            if (library == 0)
                library = NifNativePortableUnixApi.IsProcessHost ? bridge : NifNativePortableUnixApi.Open(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(path) + '\0'), global: true);
            foreach (string name in NifNativeNames.BridgeSymbols)
                if (NifNativePortableUnixApi.Symbol(0, name) != NativeLibrary.GetExport(bridge, name))
                    throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
            var register = (delegate* unmanaged[Cdecl]<NifNativeBridgeTable*, int>)NativeLibrary.GetExport(bridge, NifNativeNames.BridgeRegister);
            var reader = (delegate* unmanaged[Cdecl]<nint, nuint*, int>)NativeLibrary.GetExport(bridge, NifNativeNames.BridgeReadTerm);
            nint allocator = runtime.IndependentEnvironmentAllocator709.Pointer;
            var candidate = (NifNativeBridgeTable*)NativeMemory.Alloc((nuint)sizeof(NifNativeBridgeTable));
            if (candidate == null)
                throw new OutOfMemoryException();
            try
            {
                *candidate = NifNativeExportRegistration.CreateTable();
                NifNativeReferenceBridgeTable.Bind(ref *candidate, allocator);
                if (register(candidate) != 0)
                    throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
            }
            catch
            {
                NativeMemory.Free(candidate);
                throw;
            }
            // Retain immediately after native publication, including later failures.
            table = candidate;
            owner = runtime;
            NifNativeVariadicCursor.Reader = reader;
            try
            {
                if (NifNativePlatformPolicy.UseLinuxNamespace)
                    NifNativeLinuxPthreadControl.Bind(library);
                mutexes = NifNativeLinuxMutex795.TryBind(runtime.NativeAllocationDomain, library);
            }
            catch (Exception failure)
            {
                initializationFailure = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure);
                throw;
            }
        }
    }
}
