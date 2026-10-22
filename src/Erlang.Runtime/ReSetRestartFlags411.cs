using System.Runtime.InteropServices;

namespace Erlang;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int ReSetRestartFlags411(IntPtr data, uint flags);
