using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang.NifNativeBackend;

public static unsafe class OptionalNative793Checks
{
    public static void ProtocolWindows()
    {
        if (!OperatingSystem.IsWindows() || IntPtr.Size != 8)
            throw new PlatformNotSupportedException(OptionalNative793Names.Windows);
        Span<nint> entries = stackalloc nint[NifNativeWindowsCallbackLayout.FunctionCount + NifNativeWindowsCallbackLayout.TailPointers];
        entries.Clear();
        int* state = stackalloc int[OptionalNative793Names.StateWords];
        state[0] = state[1] = 0;
        fixed (nint* slots = entries)
        {
            NifNativeOptional793Registration.InstallWindows(slots);
            var option = (delegate* unmanaged[Cdecl]<nint, int, int>)slots[NifNativeOptional793Names.SetOptionSlot];
            var optionWithCallback = (delegate* unmanaged[Cdecl]<nint, int, nint, int>)slots[NifNativeOptional793Names.SetOptionSlot];
            var open = (delegate* unmanaged[Cdecl]<byte*, nint, nint, nint>)slots[NifNativeOptional793Names.DlopenSlot];
            var symbol = (delegate* unmanaged[Cdecl]<nint, byte*, nint, nint, nint>)slots[NifNativeOptional793Names.DlsymSlot];
            Require((nint)option != 0 && (nint)open != 0 && (nint)symbol != 0);
            Require(option(0, 1) == NifNativeOptional793Names.InvalidArgument);
            Require(optionWithCallback(0, 2, 0) == NifNativeOptional793Names.InvalidArgument);
            Require(optionWithCallback(0, 3, 0) == NifNativeOptional793Names.InvalidArgument);
            Require(option(0, int.MaxValue) == NifNativeOptional793Names.InvalidArgument);
            nint error = (nint)(delegate* unmanaged[Cdecl]<nint, byte*, void>)&ObserveError;
            Require(open(null, error, (nint)state) == 0);
            Require(symbol(
                0,
                null,
                error,
                (nint)state
            ) == 0);
            Require(state[0] == 2 && state[1] == 3);
            Require(open(null, 0, 0) == 0 && symbol(
                0,
                null,
                0,
                0
            ) == 0);
            Require(state[0] == 2);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ObserveError(nint argument, byte* reason)
    {
        int* state = (int*)argument;
        try
        {
            string? message = Marshal.PtrToStringUTF8((nint)reason);
            state[0]++;
            if (message == NifNativeOptional793Names.AuxiliaryLoadingUnsupported) state[1] |= 1;
            if (message == NifNativeOptional793Names.AuxiliarySymbolUnsupported) state[1] |= 2;
        }
        catch
        {
            state[1] |= 4;
        }
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(OptionalNative793Names.Failure);
    }
}
