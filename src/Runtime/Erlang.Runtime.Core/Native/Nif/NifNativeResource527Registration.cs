namespace Erlang;

internal static unsafe class NifNativeResource527Registration
{
    internal static void InstallWindows527(nint* slots)
    {
        slots[NifNativeResource527Layout.OpenResourceType527] = (nint)(delegate* unmanaged[Cdecl]<nint, byte*, byte*, nint, int, int*, nint>)&NifNativeResourceType527Callbacks.Open;
        slots[NifNativeResource527Layout.AllocateResource527] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nint>)&NifNativeResource527Callbacks.Allocate;
        slots[NifNativeResource527Layout.ReleaseResource527] = (nint)(delegate* unmanaged[Cdecl]<nint, void>)&NifNativeResource527Callbacks.Release;
        slots[NifNativeResource527Layout.MakeResource527] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, nuint>)&NifNativeResource527Callbacks.Make;
        slots[NifNativeResource527Layout.GetResource527] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nint, nint*, int>)&NifNativeResource527Callbacks.Get;
        slots[NifNativeResource527Layout.SizeOfResource527] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint>)&NifNativeResource527Callbacks.Size;
        slots[NifNativeResource527Layout.KeepResource527] = (nint)(delegate* unmanaged[Cdecl]<nint, void>)&NifNativeResource527Callbacks.Keep;
        slots[NifNativeResource527Layout.MakeResourceBinary527] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, nuint, nuint>)&NifNativeResource527Callbacks.MakeBinary;
        slots[NifNativeResource527Layout.OpenResourceTypeX527] = (nint)(delegate* unmanaged[Cdecl]<nint, byte*, nint, int, int*, nint>)&NifNativeResourceType527Callbacks.OpenX;
        slots[NifNativeResource527Layout.InitResourceType527] = (nint)(delegate* unmanaged[Cdecl]<nint, byte*, nint, int, int*, nint>)&NifNativeResourceType527Callbacks.Init;
        slots[NifNativeResource527Layout.DynamicResourceCall527] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nuint, nuint, nint, int>)&NifNativeResourceType527Callbacks.DynamicCall;
    }
}
