using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Erlang;

// Explicit partial feature boundary. No successful option registration or
// auxiliary loader handle is fabricated. Ordinary NIF loading stays separate.
internal static unsafe class NifNativeOptional793Callbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int SetOption(nint environment, int option)
    {
        // All current lifecycle options need scheduler/halt dispatch not yet
        // implemented. Reject before consuming optional varargs or storing a
        // callback. Never return zero for a silently ignored registration.
        return NifNativeOptional793Names.InvalidArgument;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nint Open(byte* filename, nint errorHandler, nint errorArgument)
    {
        Report(errorHandler, errorArgument, NifNativeOptional793Names.AuxiliaryLoadingUnsupported);

        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nint Symbol(
        nint handle,
        byte* name,
        nint errorHandler,
        nint errorArgument
    )
    {
        Report(errorHandler, errorArgument, NifNativeOptional793Names.AuxiliarySymbolUnsupported);

        return 0;
    }

    private static void Report(nint errorHandler, nint errorArgument, string message)
    {
        if (errorHandler == 0) return;
        try
        {
            byte[] terminated = Encoding.UTF8.GetBytes(message + '\0');
            fixed (byte* text = terminated)
                ((delegate* unmanaged[Cdecl]<nint, byte*, void>)errorHandler)(errorArgument, text);
        }
        catch (Exception failure)
        {
            // Only exceptional report construction/managed callback failure is
            // recorded. Ordinary unsupported-feature NULL is not a CLR throw.
            NifNativeCallbackScope.Fail(failure);
        }
    }
}
