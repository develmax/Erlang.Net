using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

internal static unsafe class NativeResourceMagic527
{
    internal const nuint Magic = 1;
    internal const int ReleaseX64PointerBytes = 8;
    internal static int ResourceOffset527 => sizeof(NativeResourceMagicHeader527);
    // Binary.orig_bytes starts after intern.flags/apparent_size/refc/orig_size.
    internal static int BinaryBytesOffset527 => sizeof(nuint) * 4;
    internal static int UserOffset527 => ResourceOffset527 + sizeof(NativeResourceHeader524);
    internal static nint Destructor527 => (nint)(delegate* unmanaged[Cdecl]<nint, int>)&Destroy;

    internal static void ValidateProfile527()
    {
        if (IntPtr.Size != ReleaseX64PointerBytes)
            throw new PlatformNotSupportedException(NativeResource527Names.PhysicalProfile);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int Destroy(nint binary)
    {
        NativeResourcePayload524? payload = null;
        try
        {
            payload = NativeResourceAddresses524.Resolve524(binary + UserOffset527);
            payload.Type.Destroy524(payload.IdentityPointer524);

            return 1;
        }
        catch (Exception error)
        {
            // Never throw a managed exception across an unmanaged boundary.
            // Preserve the actual failed callback for the retirement receipt.
            if (payload is not null) payload.DestructorFailure527 = error;
            else NifNativeCallbackScope.Fail(error);
        }

        return 0;
    }
}
