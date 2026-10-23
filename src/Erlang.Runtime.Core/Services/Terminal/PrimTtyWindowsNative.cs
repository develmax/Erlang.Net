using System.Runtime.InteropServices;

namespace Erlang;

internal static class PrimTtyWindowsNative
{
    [DllImport(PrimTtyWindowsNames.KernelLibrary, SetLastError = true)]
    internal static extern nint GetStdHandle(int number);

    [DllImport(PrimTtyWindowsNames.KernelLibrary, SetLastError = true)]
    internal static extern uint GetFileType(nint handle);

    [DllImport(PrimTtyWindowsNames.KernelLibrary, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetConsoleMode(nint handle, out uint mode);

    [DllImport(PrimTtyWindowsNames.KernelLibrary, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern nint CreateFileW(
        string name,
        uint access,
        uint share,
        nint security,
        uint disposition,
        uint flags,
        nint template
    );

    [DllImport(PrimTtyWindowsNames.KernelLibrary, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseHandle(nint handle);

    [DllImport(PrimTtyWindowsNames.KernelLibrary, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool PeekNamedPipe(
        nint handle,
        nint buffer,
        uint size,
        nint read,
        out uint available,
        nint left
    );

    [DllImport(PrimTtyWindowsNames.KernelLibrary, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetNumberOfConsoleInputEvents(nint handle, out uint count);

    [DllImport(PrimTtyWindowsNames.KernelLibrary, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetConsoleScreenBufferInfoEx(nint handle, ref ScreenInfo value);

    [DllImport(PrimTtyWindowsNames.KernelLibrary, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ReadFile(
        nint handle,
        byte[] buffer,
        uint count,
        out uint read,
        nint overlapped
    );

    [DllImport(PrimTtyWindowsNames.KernelLibrary, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ReadConsoleW(
        nint handle,
        [Out] byte[] buffer,
        uint characters,
        out uint read,
        nint control
    );

    [DllImport(PrimTtyWindowsNames.KernelLibrary, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool WriteFile(
        nint handle,
        byte[] buffer,
        uint count,
        out uint written,
        nint overlapped
    );

    [DllImport(PrimTtyWindowsNames.KernelLibrary)]
    internal static extern uint GetCurrentThreadId();

    [DllImport(PrimTtyWindowsNames.KernelLibrary, SetLastError = true)]
    internal static extern nint OpenThread(uint access, bool inherit, uint id);

    [DllImport(PrimTtyWindowsNames.KernelLibrary, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CancelSynchronousIo(nint thread);

    [DllImport(PrimTtyWindowsNames.KernelLibrary, SetLastError = true)]
    internal static extern uint FormatMessageA(
        uint flags,
        nint source,
        uint code,
        uint language,
        out nint buffer,
        uint size,
        nint arguments
    );

    [DllImport(PrimTtyWindowsNames.KernelLibrary)]
    internal static extern nint LocalFree(nint memory);

    [DllImport(PrimTtyWindowsNames.CrtLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint _get_current_locale();

    [DllImport(PrimTtyWindowsNames.CrtLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int _isprint_l(int value, nint locale);

    [DllImport(PrimTtyWindowsNames.CrtLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void _free_locale(nint locale);

    [StructLayout(LayoutKind.Sequential)]
    internal struct Coord { internal short X, Y; }
    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect { internal short Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    internal struct ScreenInfo
    {
        internal uint Size;
        internal Coord BufferSize, Cursor;
        internal ushort Attributes;
        internal Rect Window;
        internal Coord Maximum;
        internal ushort Popup;
        internal int Fullscreen;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        internal uint[] Colors;
    }
}
