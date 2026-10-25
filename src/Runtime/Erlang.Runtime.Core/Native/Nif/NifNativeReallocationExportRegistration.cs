using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Erlang;

internal static unsafe class NifNativeReallocationExportRegistration
{
    private static readonly object Gate = new();
    private static nint library;
    private static string? trustedHash;
    private static FileStream? verifiedFile;
    // Immutable process-owned extension: imported addresses may outlive every image.
    private static NifNativeReallocationBridgeTable* table;

    internal static void EnsureRegistered(NifNativeCapabilityProfile profile)
    {
        if (!profile.ReallocationEnabled)
            throw new InvalidOperationException(NifNativeDiagnostics.ReallocationNotGranted);
        if (OperatingSystem.IsWindows())
            return;
        if (AssemblyLoadContext.GetLoadContext(typeof(NifNativeReallocationExportRegistration).Assembly)?.IsCollectible == true)
            throw new NotSupportedException(NifNativeDiagnostics.BridgeLifetime);
        if (profile.ReallocationBridgePath is not { } path || profile.ReallocationBridgeSha256 is not { } hash)
            throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
        lock (Gate)
        {
            if (library != 0 && table != null && verifiedFile is not null)
            {
                if (!string.Equals(trustedHash, hash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);

                return;
            }
            // A previously published unrelated global symbol cannot be borrowed as authority.
            nint prior;
            try
            {
                prior = NifNativePlatformLoader.Symbol(0, NifNativeReallocationNames.Reallocate);
            }
            catch (InvalidOperationException failure) when (failure.Message == NifNativeDiagnostics.BridgeContract)
            {
                prior = 0;
            }
            if (prior != 0)
                throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
            var file = NifNativeCapabilityProfile.OpenVerified(path, hash);
            nint handle = 0;
            NifNativeReallocationBridgeTable* candidate = null;
            bool published = false;
            try
            {
                handle = NifNativePlatformLoader.Open(file, path, global: true);
                if (NifNativePlatformLoader.Symbol(0, NifNativeReallocationNames.Reallocate)
                    != NifNativePlatformLoader.Symbol(handle, NifNativeReallocationNames.Reallocate))
                    throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
                var register = (delegate* unmanaged[Cdecl]<NifNativeReallocationBridgeTable*, int>)NifNativePlatformLoader.Symbol(handle, NifNativeReallocationNames.Register);
                candidate = (NifNativeReallocationBridgeTable*)NativeMemory.Alloc((nuint)sizeof(NifNativeReallocationBridgeTable));
                if (candidate == null)
                    throw new OutOfMemoryException();
                *candidate = new NifNativeReallocationBridgeTable
                {
                    Version = NifNativeReallocationNames.Version,
                    Size = (uint)sizeof(NifNativeReallocationBridgeTable),
                    Reallocate = &NifNativePrimitiveCallbacks.Reallocate
                };
                if (register(candidate) != 0)
                    throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
                published = true;
                table = candidate;
                trustedHash = hash;
                verifiedFile = file;
                library = handle;
            }
            finally
            {
                if (!published)
                {
                    NativeMemory.Free(candidate);
                    try
                    {
                        NifNativePlatformLoader.Close(handle);
                    }
                    finally
                    {
                        file.Dispose();
                    }
                }
            }
        }
    }
}
