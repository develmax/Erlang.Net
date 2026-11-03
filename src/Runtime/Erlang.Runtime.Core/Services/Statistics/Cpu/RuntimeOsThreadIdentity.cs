using System.Runtime.InteropServices;

namespace Erlang;

internal static class RuntimeOsThreadIdentity
{
    internal static int Current()
    {
        int value = OperatingSystem.IsWindows()
            ? checked((int)PrimTtyWindowsNative.GetCurrentThreadId())
            : OperatingSystem.IsLinux() ? GetTid()
            : throw new PlatformNotSupportedException(RuntimeCpuTimeNames.Platform);
        if (value <= 0)
            throw new InvalidOperationException(RuntimeCpuTimeNames.ThreadMissing);

        return value;
    }

    [DllImport(RuntimeCpuTimeNames.Libc, EntryPoint = RuntimeCpuTimeNames.GetTid, CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetTid();
}
