using System.Runtime.InteropServices;

namespace Erlang;

internal static class ErlDriverEntryTypes420
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint DriverInitialize(nint callbacks);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int Initialize();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint Start(nint port, nint command);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void Stop(nint data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void Output(nint data, nint bytes, nuint size);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void Ready(nint data, nint nativeEvent);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void Finish();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint Control(
        nint data,
        uint command,
        nint bytes,
        nuint size,
        ref nint result,
        nuint available
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint Call(
        nint data,
        uint command,
        nint bytes,
        nuint size,
        ref nint result,
        nuint available,
        ref uint flags
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void Unary(nint data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void Binary(nint data, nint argument);
}
