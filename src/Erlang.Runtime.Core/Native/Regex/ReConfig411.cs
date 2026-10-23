using System.Runtime.InteropServices;

namespace Erlang;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int ReConfig411(uint what, IntPtr result);
