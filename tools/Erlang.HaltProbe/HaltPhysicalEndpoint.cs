using System.ComponentModel;
using System.Diagnostics;
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

    private static void WriteWindowsDump(string dump)
    {
        var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writer = new Thread(() =>
        {
            try
            {
                using var process = Process.GetCurrentProcess();
                using var file = new FileStream(
                    dump,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None
                );
                if (!MiniDumpWriteDump(
                    process.Handle,
                    (uint)process.Id,
                    file.SafeFileHandle.DangerousGetHandle(),
                    HaltProbeNames.NormalDump,
                    0,
                    0,
                    0
                ))
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                file.Flush(true);
                complete.SetResult();
            }
            catch (Exception error)
            {
                complete.SetException(error);
            }
        });
        writer.Start();
        writer.Join();
        complete.Task.GetAwaiter().GetResult();
    }

    [DllImport(HaltProbeNames.WindowsRuntime, EntryPoint = "abort", CallingConvention = CallingConvention.Cdecl)]
    private static extern void WindowsAbort();

    [DllImport(HaltProbeNames.WindowsRuntime, EntryPoint = "_set_abort_behavior", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint SetAbortBehavior(uint flags, uint mask);

    [DllImport(HaltProbeNames.UnixRuntime, EntryPoint = "abort", CallingConvention = CallingConvention.Cdecl)]
    private static extern void UnixAbort();

    [DllImport(HaltProbeNames.WindowsDumpLibrary, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MiniDumpWriteDump(
        nint process,
        uint processId,
        nint file,
        int type,
        nint exceptions,
        nint userStreams,
        nint callbacks
    );
}
