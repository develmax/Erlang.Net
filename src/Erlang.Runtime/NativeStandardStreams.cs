using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Erlang;

internal static class NativeStandardStreams
{
    public static Stream Open(bool standardError)
    {
        if (!OperatingSystem.IsWindows())
            return standardError ? Console.OpenStandardError() : Console.OpenStandardOutput();
        nint handle = GetStdHandle(standardError ? WindowsStandardStreamParameters.ErrorHandle : WindowsStandardStreamParameters.OutputHandle);
        if (handle == nint.Zero || handle == WindowsStandardStreamParameters.InvalidHandle)
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        var safe = new SafeFileHandle(handle, ownsHandle: false);
        try
        {
            return new FileStream(
                safe,
                FileAccess.Write,
                WindowsStandardStreamParameters.UnbufferedWriteSize,
                isAsync: false
            );
        }
        catch
        {
            safe.Dispose();
            throw;
        }
    }

    [DllImport(WindowsStandardStreamParameters.NativeLibrary, EntryPoint = WindowsStandardStreamParameters.HandleEntryPoint, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern nint GetStdHandle(int device);
}
