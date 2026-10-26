using System.Runtime.InteropServices;

namespace Erlang;

internal static class ErlDriverThreadBridgeTypes420
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int Reserve(ulong owner, nint reservation);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int Publish(ulong reservation, nint thread);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void Abort(ulong reservation);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int Configure(Reserve reserve, Publish publish, Abort abort);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate ulong ExchangeOwner(ulong owner);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int Callbacks(nint table, nuint size);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int Close();
}
