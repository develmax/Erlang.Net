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
            int error;
            fixed (byte* pointer = buffer)
            {
                if (NifNativePlatformPolicy.UseLinuxNamespace)
                {
                    if (GetWorkingDirectory(pointer, (nuint)buffer.Length) != 0)
                        return buffer[..Array.IndexOf(buffer, (byte)0)];
                    error = Marshal.GetLastPInvokeError();
                }
                else
                {
                    int result = NifNativePortableUnixApi.WorkingDirectory(pointer, (nuint)buffer.Length);
                    if (result == 0)
                        return buffer[..Array.IndexOf(buffer, (byte)0)];
                    error = -result;
                }
            }
            if (error != NifNativeUnixPathNames.RangeError)
                throw new IOException(new Win32Exception(error).Message, new Win32Exception(error));
            size = checked(size * 2);
        }
    }

    internal static FileStream OpenRead(NifNativeLibraryPath path)
    {
        byte[] bytes = path.UnixNullTerminated();
        int descriptor;
        bool linux = NifNativePlatformPolicy.UseLinuxNamespace;
        fixed (byte* pointer = bytes)
            descriptor = linux ? Open(pointer, NifNativeUnixPathNames.CloseOnExec)
                : NifNativePortableUnixApi.OpenFile(pointer);
        if (descriptor < 0)
        {
            int error = linux ? Marshal.GetLastPInvokeError() : -descriptor;
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
