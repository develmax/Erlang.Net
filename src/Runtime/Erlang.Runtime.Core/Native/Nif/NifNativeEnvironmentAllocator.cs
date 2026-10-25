using System.Runtime.InteropServices;

namespace Erlang;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate nint NifNativeEnvironmentAllocator();
