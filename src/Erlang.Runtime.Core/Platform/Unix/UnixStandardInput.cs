using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Erlang;

internal sealed class UnixStandardInput : IDisposable
{
    private readonly FileStream terminal;
    private readonly nuint request;

    private UnixStandardInput(FileStream terminal, nuint request)
    {
        this.terminal = terminal;
        this.request = request;
    }

    public static UnixStandardInput Open()
    {
        nuint request = OperatingSystem.IsLinux() ? UnixStandardInputParameters.LinuxInputInjectionRequest :
            OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD() ? UnixStandardInputParameters.BsdInputInjectionRequest :
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var terminal = new FileStream(UnixStandardInputParameters.TerminalPath, FileMode.Open, FileAccess.Read);

        return new(terminal, request);
    }

    public void Write(ReadOnlySpan<byte> bytes)
    {
        int descriptor = terminal.SafeFileHandle.DangerousGetHandle().ToInt32();
        foreach (byte item in bytes)
        {
            byte value = item;
            if (Ioctl(descriptor, request, ref value) < 0)
                throw new Win32Exception(Marshal.GetLastPInvokeError());
        }
    }

    [DllImport(UnixStandardInputParameters.NativeLibrary, EntryPoint = UnixStandardInputParameters.IoctlEntryPoint, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int Ioctl(int descriptor, nuint request, ref byte value);

    public void Dispose() => terminal.Dispose();
}
