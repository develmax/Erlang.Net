namespace Erlang;

internal static unsafe class NifNativeOptional793Registration
{
    internal static void InstallWindows(nint* slots)
    {
        slots[NifNativeOptional793Names.DlopenSlot] = (nint)(delegate* unmanaged[Cdecl]<byte*, nint, nint, nint>)&NifNativeOptional793Callbacks.Open;
        slots[NifNativeOptional793Names.DlsymSlot] = (nint)(delegate* unmanaged[Cdecl]<nint, byte*, nint, nint, nint>)&NifNativeOptional793Callbacks.Symbol;
        // Windows x64 passes the same first two integer arguments for variadic
        // set_option; this rejecting entry never reads any optional argument.
        slots[NifNativeOptional793Names.SetOptionSlot] = (nint)(delegate* unmanaged[Cdecl]<nint, int, int>)&NifNativeOptional793Callbacks.SetOption;
    }
}
