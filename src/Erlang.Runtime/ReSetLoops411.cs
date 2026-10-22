using System.Runtime.InteropServices;

namespace Erlang;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void ReSetLoops411(IntPtr data, int budget);
