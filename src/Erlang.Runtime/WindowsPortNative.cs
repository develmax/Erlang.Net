using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Erlang;

internal static class WindowsPortNative
{
    [DllImport(WindowsPortParameters.Library, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern uint SearchPathW(
        string? path,
        string file,
        string? extension,
        int length,
        StringBuilder result,
        nint filePart
    );

    [DllImport(WindowsPortParameters.Library, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern SafeFileHandle CreateNamedPipeW(
        string name,
        uint openMode,
        uint pipeMode,
        uint instances,
        uint outputBytes,
        uint inputBytes,
        uint timeout,
        ref WindowsPortSecurity security
    );

    [DllImport(WindowsPortParameters.Library, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern SafeFileHandle CreateFileW(
        string name,
        uint access,
        uint sharing,
        ref WindowsPortSecurity security,
        uint disposition,
        uint flags,
        nint template
    );

    [DllImport(WindowsPortParameters.Library, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool InitializeProcThreadAttributeList(
        nint list,
        int count,
        uint flags,
        ref nuint bytes
    );

    [DllImport(WindowsPortParameters.Library, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UpdateProcThreadAttribute(
        nint list,
        uint flags,
        nuint attribute,
        nint value,
        nuint bytes,
        nint previous,
        nint returnedBytes
    );

    [DllImport(WindowsPortParameters.Library)]
    public static extern void DeleteProcThreadAttributeList(nint list);

    [DllImport(WindowsPortParameters.Library, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CreateProcessW(
        string application,
        StringBuilder commandLine,
        nint processSecurity,
        nint threadSecurity,
        [MarshalAs(UnmanagedType.Bool)] bool inherit,
        uint creationFlags,
        nint environment,
        string? directory,
        ref WindowsPortStartup startup,
        out WindowsPortProcessInformation process
    );

    [DllImport(WindowsPortParameters.Library)]
    public static extern nint GetCurrentProcess();

    [DllImport(WindowsPortParameters.Library)]
    public static extern nint GetStdHandle(int kind);

    [DllImport(WindowsPortParameters.Library, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DuplicateHandle(
        nint sourceProcess,
        nint source,
        nint targetProcess,
        out SafeFileHandle target,
        uint access,
        [MarshalAs(UnmanagedType.Bool)] bool inherit,
        uint flags
    );

    [DllImport(WindowsPortParameters.Library, SetLastError = true)]
    public static extern uint WaitForSingleObject(SafeProcessHandle process, uint milliseconds);

    [DllImport(WindowsPortParameters.Library, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetExitCodeProcess(SafeProcessHandle process, out uint code);
}
