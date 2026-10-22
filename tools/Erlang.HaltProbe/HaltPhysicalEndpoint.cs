using System.Runtime.InteropServices;

namespace Erlang.HaltProbe;

internal static class HaltPhysicalEndpoint
{
    public static int Finish(RuntimeHaltResult result, string? dump)
    {
        if (result.Kind == RuntimeHaltKind.Numeric)
            return result.Status;
        if (result.Kind == RuntimeHaltKind.Abort)
        {
            if (OperatingSystem.IsWindows())
            {
                SetAbortBehavior(0, HaltProbeNames.CallReportFault);
                WindowsAbort();
            }
            else
                UnixAbort();
            throw new InvalidOperationException(HaltProbeNames.Abort);
        }
        if (dump is null)
            throw new ArgumentException(HaltProbeNames.Dump);
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException(HaltProbeNames.DumpFormatGap);
        if (OperatingSystem.IsLinux())
            LinuxOwnedDump.Write(dump);
        else
            WriteWindowsDump(dump);
        string slogan = result.Slogan ?? string.Empty;
        int nul = slogan.IndexOf('\0');
        if (nul >= 0)
            slogan = slogan[..nul];
        Console.Error.WriteLine(slogan);

        return result.Status;
    }

    private static void WriteWindowsDump(string dump) => HaltWindowsDumpParent437.Write(dump);

    [DllImport(HaltProbeNames.WindowsRuntime, EntryPoint = "abort", CallingConvention = CallingConvention.Cdecl)]
    private static extern void WindowsAbort();

    [DllImport(HaltProbeNames.WindowsRuntime, EntryPoint = "_set_abort_behavior", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint SetAbortBehavior(uint flags, uint mask);

    [DllImport(HaltProbeNames.UnixRuntime, EntryPoint = "abort", CallingConvention = CallingConvention.Cdecl)]
    private static extern void UnixAbort();

}
