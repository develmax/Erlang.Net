using System.Runtime.InteropServices;

namespace Erlang;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int ReSerializeDecode413(
    out IntPtr code,
    int count,
    IntPtr bytes,
    IntPtr context
);
