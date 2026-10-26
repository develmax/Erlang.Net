using System.Runtime.InteropServices;

namespace Erlang;

internal static class ReNative411
{
    [DllImport(ReProtocolNames411.Kernel32Dll, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern IntPtr LoadLibraryExW(string file, IntPtr reserved, uint flags);

    [DllImport(ReProtocolNames411.Kernel32Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool FreeLibrary(IntPtr module);

    [DllImport(ReProtocolNames411.Kernel32Dll, CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    internal static extern IntPtr GetProcAddress(IntPtr module, string name);

    [DllImport(ReProtocolNames411.Kernel32Dll, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetModuleHandleExW(uint flags, IntPtr address, out IntPtr module);
}
