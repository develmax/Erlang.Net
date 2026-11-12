using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Erlang;

internal static class RuntimeThreadUserCpuNative
{
    [DllImport(RuntimeCpuTimeNames.WindowsLibrary, SetLastError = true)]
    internal static extern SafeFileHandle OpenThread(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint id);

    [DllImport(RuntimeCpuTimeNames.WindowsLibrary, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetThreadTimes(
        SafeFileHandle thread,
        out long creation,
        out long exit,
        out long kernel,
        out long user
    );
}
