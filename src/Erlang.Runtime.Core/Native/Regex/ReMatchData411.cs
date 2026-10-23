using System.Runtime.InteropServices;

namespace Erlang;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate IntPtr ReMatchData411(IntPtr code, IntPtr context);
