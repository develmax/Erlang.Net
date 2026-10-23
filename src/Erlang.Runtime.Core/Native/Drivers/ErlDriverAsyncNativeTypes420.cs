using System.Runtime.InteropServices;

namespace Erlang;

internal static class ErlDriverAsyncNativeTypes420
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void Complete(ulong identity);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int Create(uint count, Complete complete, out nint pool);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int Submit(
        nint pool,
        uint lane,
        ulong owner,
        ulong identity,
        nint invoke,
        nint data
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int Close(nint pool);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void Work(nint data);
}
