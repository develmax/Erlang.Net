using System.Runtime.InteropServices;

namespace Erlang;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int RePatternInfo411(IntPtr code, uint what, IntPtr result);
