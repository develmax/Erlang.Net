using System.Runtime.InteropServices;
using System.Text;

namespace Erlang.PortProbe;

internal static class PortProbeNativeArguments
{
    public static byte[] First()
    {
        if (!OperatingSystem.IsWindows())
        {
            byte[] bytes = File.ReadAllBytes(PortProbeParameters.ProcessCommandLine);
            int end = Array.IndexOf(bytes, (byte)0);

            return bytes[..end];
        }
        nint arguments = CommandLineToArgvW(GetCommandLineW(), out _);
        if (arguments == 0)
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError());
        try
        {
            return Encoding.UTF8.GetBytes(Marshal.PtrToStringUni(Marshal.ReadIntPtr(arguments))!);
        }
        finally
        {
            LocalFree(arguments);
        }
    }

    [DllImport(PortProbeParameters.WindowsKernel)]
    private static extern nint GetCommandLineW();

    [DllImport(PortProbeParameters.WindowsShell, SetLastError = true)]
    private static extern nint CommandLineToArgvW(nint command, out int count);

    [DllImport(PortProbeParameters.WindowsKernel)]
    private static extern nint LocalFree(nint memory);
}
