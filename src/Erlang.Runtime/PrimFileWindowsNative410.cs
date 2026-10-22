using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Erlang;

internal static class PrimFileWindowsNative410
{
    [DllImport(PrimFileWindowsNames410.Kernel, EntryPoint = PrimFileWindowsNames410.FullPath, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    internal static extern uint FullPath(
        string input,
        uint capacity,
        [Out] char[]? result,
        IntPtr part
    );

    [DllImport(PrimFileWindowsNames410.Kernel, EntryPoint = PrimFileWindowsNames410.Create, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    internal static extern SafeFileHandle Open(
        string path,
        uint access,
        uint sharing,
        IntPtr security,
        uint mode,
        uint flags,
        IntPtr template
    );

    [DllImport(PrimFileWindowsNames410.Kernel, EntryPoint = PrimFileWindowsNames410.Attributes, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    internal static extern uint Attributes(string path);

    [DllImport(PrimFileWindowsNames410.Kernel, EntryPoint = PrimFileWindowsNames410.Information, SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool Information(SafeFileHandle handle, out PrimFileInformation410 result);

    [DllImport(PrimFileWindowsNames410.Kernel, EntryPoint = PrimFileWindowsNames410.Read, SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static unsafe extern bool Read(
        SafeFileHandle handle,
        byte* output,
        uint count,
        out uint read,
        IntPtr overlapped
    );

    [DllImport(PrimFileWindowsNames410.Kernel, EntryPoint = PrimFileWindowsNames410.First, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    internal static extern IntPtr First(string path, out PrimFileFindData410 result);

    [DllImport(PrimFileWindowsNames410.Kernel, EntryPoint = PrimFileWindowsNames410.Next, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool Next(IntPtr search, out PrimFileFindData410 result);

    [DllImport(PrimFileWindowsNames410.Kernel, EntryPoint = PrimFileWindowsNames410.CloseSearch, SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseSearch(IntPtr search);

    [DllImport(PrimFileWindowsNames410.Kernel, EntryPoint = PrimFileWindowsNames410.FinalPath, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    internal static extern uint FinalPath(
        SafeFileHandle handle,
        [Out] char[]? result,
        uint capacity,
        uint flags
    );

    [DllImport(PrimFileWindowsNames410.Kernel, EntryPoint = PrimFileWindowsNames410.DeviceControl, SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeviceControl(
        SafeFileHandle handle,
        uint code,
        IntPtr input,
        uint inputSize,
        [Out] byte[] output,
        uint outputSize,
        out uint returned,
        IntPtr overlapped
    );

    [DllImport(PrimFileWindowsNames410.Runtime, EntryPoint = PrimFileWindowsNames410.WorkingDirectory, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, ExactSpelling = true)]
    internal static extern IntPtr WorkingDirectory(int drive, [Out] char[] output, int capacity);
}
