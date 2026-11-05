using System.Runtime.InteropServices;

namespace Erlang;

internal static class PrimFileWindowsDeleteNative
{
    [DllImport(PrimFileWindowsNames410.Kernel, EntryPoint = PrimFileDeleteNames.WindowsEntry, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool Delete(string path);
}
