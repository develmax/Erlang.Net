using System.Runtime.InteropServices;

namespace Erlang;

internal static class PrimFilePosixNative
{
    [DllImport(PrimFilePosixNames.Library, EntryPoint = PrimFilePosixNames.Abi, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern int Abi();

    [DllImport(PrimFilePosixNames.Library, EntryPoint = PrimFilePosixNames.Open, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern int Open(byte[] path, int flags, out int fd);

    [DllImport(PrimFilePosixNames.Library, EntryPoint = PrimFilePosixNames.Read, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern int Read(
        int fd,
        [Out] byte[] buffer,
        int count,
        int positioned,
        long offset,
        out int actual
    );

    [DllImport(PrimFilePosixNames.Library, EntryPoint = PrimFilePosixNames.Write, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern int Write(
        int fd,
        byte[] buffer,
        int count,
        int positioned,
        long offset
    );

    [DllImport(PrimFilePosixNames.Library, EntryPoint = PrimFilePosixNames.Seek, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern int Seek(
        int fd,
        long offset,
        int origin,
        out long position
    );

    [DllImport(PrimFilePosixNames.Library, EntryPoint = PrimFilePosixNames.Truncate, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern int Truncate(int fd);

    [DllImport(PrimFilePosixNames.Library, EntryPoint = PrimFilePosixNames.Sync, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern int Sync(int fd, int dataOnly);

    [DllImport(PrimFilePosixNames.Library, EntryPoint = PrimFilePosixNames.Path, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern int Path(int operation, byte[] path, byte[]? target);

    [DllImport(PrimFilePosixNames.Library, EntryPoint = PrimFilePosixNames.Stat, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern int Stat(
        byte[]? path,
        int fd,
        int follow,
        out PrimFilePosixInfo info
    );

    [DllImport(PrimFilePosixNames.Library, EntryPoint = PrimFilePosixNames.Cwd, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern int Cwd([Out] byte[] buffer, int capacity, out int length);

    [DllImport(PrimFilePosixNames.Library, EntryPoint = PrimFilePosixNames.OpenDir, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern int OpenDir(byte[] path, out nint handle);

    [DllImport(PrimFilePosixNames.Library, EntryPoint = PrimFilePosixNames.ReadDir, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern int ReadDir(
        nint handle,
        [Out] byte[] buffer,
        int capacity,
        out int length
    );

    [DllImport(PrimFilePosixNames.Library, EntryPoint = PrimFilePosixNames.CloseDir, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern void CloseDir(nint handle);

}
