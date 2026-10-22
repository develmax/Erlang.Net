using System.Runtime.InteropServices;

namespace Erlang;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int ReSetRestartData411(IntPtr data, IntPtr cell);
