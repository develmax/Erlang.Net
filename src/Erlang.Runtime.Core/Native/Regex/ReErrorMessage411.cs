using System.Runtime.InteropServices;

namespace Erlang;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int ReErrorMessage411(int error, IntPtr buffer, nuint size);
