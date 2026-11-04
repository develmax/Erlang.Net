using System.Runtime.InteropServices;

namespace Erlang;

internal static class PrimFileUnixDeleteNative
{
    [DllImport(PrimFileDeleteNames.UnixLibrary, EntryPoint = PrimFileDeleteNames.UnixEntry, CallingConvention = CallingConvention.Cdecl, SetLastError = true, ExactSpelling = true)]
    internal static extern int Unlink([In] byte[] path);
}
