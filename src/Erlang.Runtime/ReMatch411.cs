using System.Runtime.InteropServices;

namespace Erlang;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int ReMatch411(
    IntPtr code,
    IntPtr subject,
    nuint length,
    nuint offset,
    uint options,
    IntPtr data,
    IntPtr context
);
