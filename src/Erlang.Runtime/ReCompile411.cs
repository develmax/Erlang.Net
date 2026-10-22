using System.Runtime.InteropServices;

namespace Erlang;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate IntPtr ReCompile411(
    IntPtr pattern,
    nuint length,
    uint options,
    out int error,
    out nuint offset,
    IntPtr context
);
