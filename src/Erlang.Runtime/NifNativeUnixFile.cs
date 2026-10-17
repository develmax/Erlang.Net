using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Erlang;

internal static unsafe class NifNativeUnixFile
{
    internal static byte[] WorkingDirectory()
    {
        int size = NifNativeUnixPathNames.InitialDirectoryBytes;
        while (true)
        {
            byte[] buffer = new byte[size];
            fixed (byte* pointer = buffer)
            {
                if (GetWorkingDirectory(pointer, (nuint)buffer.Length) != 0)
                    return buffer[..Array.IndexOf(buffer, (byte)0)];
            }
            int error = Marshal.GetLastPInvokeError();
            if (error != NifNativeUnixPathNames.RangeError)
                throw new IOException(new Win32Exception(error).Message, new Win32Exception(error));
            size = checked(size * 2);
        }
    }

    internal static FileStream OpenRead(NifNativeLibraryPath path)
    {
        byte[] bytes = path.UnixNullTerminated();
        int descriptor;
        fixed (byte* pointer = bytes)
            descriptor = Open(pointer, NifNativeUnixPathNames.CloseOnExec);
        if (descriptor < 0)
        {
            int error = Marshal.GetLastPInvokeError();
            var failure = new Win32Exception(error);
            throw error switch
            {
                NifNativeUnixPathNames.MissingFile => new FileNotFoundException(failure.Message, failure),
                NifNativeUnixPathNames.NotDirectory => new DirectoryNotFoundException(failure.Message, failure),
                NifNativeUnixPathNames.AccessDenied => new UnauthorizedAccessException(failure.Message, failure),
                _ => new IOException(failure.Message, failure)
            };
        }
        var handle = new SafeFileHandle((nint)descriptor, ownsHandle: true);
        try
        {
            return new FileStream(handle, FileAccess.Read);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    [DllImport(NifNativeUnixPathNames.Library, EntryPoint = NifNativeUnixPathNames.GetWorkingDirectory,
        CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    private static extern nint GetWorkingDirectory(byte* buffer, nuint size);

    [DllImport(NifNativeUnixPathNames.Library, EntryPoint = NifNativeUnixPathNames.Open,
        CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    private static extern int Open(byte* path, int flags);
}
