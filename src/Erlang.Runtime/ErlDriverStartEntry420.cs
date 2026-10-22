using System.Runtime.InteropServices;

namespace Erlang;

internal static class ErlDriverStartEntry420
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint Invoke(
        nint start,
        nint port,
        nint command,
        out int nativeErrno
    );
}
