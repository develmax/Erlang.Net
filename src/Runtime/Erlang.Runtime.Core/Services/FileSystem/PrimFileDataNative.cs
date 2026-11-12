using System.Runtime.InteropServices;

namespace Erlang;

internal static class PrimFileDataNative
{
    [DllImport(PrimFileDataNativeNames.Library, EntryPoint = PrimFileDataNativeNames.Delete, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool Delete(string path);
}
