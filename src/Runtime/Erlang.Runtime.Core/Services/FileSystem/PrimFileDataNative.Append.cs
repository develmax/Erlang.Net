using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Erlang;

internal static partial class PrimFileDataNative
{
    [DllImport(PrimFileDataNativeNames.Library, EntryPoint = PrimFileDataNativeNames.Write, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern unsafe bool WriteAtEnd(
        SafeFileHandle file,
        byte* bytes,
        uint count,
        out uint written,
        ref PrimFileAppendOffset offset
    );

    [DllImport(PrimFileDataNativeNames.Library, EntryPoint = PrimFileDataNativeNames.Position, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Position(
        SafeFileHandle file,
        long distance,
        out long position,
        SeekOrigin origin
    );

    // OTP win_prim_file.c efile_writev: EOF is selected in the write syscall,
    // rather than by a separate seek. Positioned writes keep their own offset.
    internal static unsafe int Append(FileStream stream, ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return 0;
        }
        SafeFileHandle handle = stream.SafeFileHandle;
        while (!bytes.IsEmpty)
        {
            var offset = new PrimFileAppendOffset
            {
                Internal = 0,
                InternalHigh = 0,
                Event = 0,
                Offset = PrimFileDataNativeNames.AppendOffsetWord,
                OffsetHigh = PrimFileDataNativeNames.AppendOffsetWord
            };
            uint written;
            fixed (byte* pointer = bytes)
            {
                if (!WriteAtEnd(
                    handle,
                    pointer,
                    (uint)bytes.Length,
                    out written,
                    ref offset
                ))
                {
                    return Marshal.GetLastPInvokeError();
                }
            }
            if (written == 0)
            {
                return PrimFileDataNativeNames.WriteFault;
            }
            bytes = bytes[(int)written..];
        }
        if (!Position(
            handle,
            0,
            out long position,
            SeekOrigin.Current
        ))
        {
            return Marshal.GetLastPInvokeError();
        }
        // Reconcile FileStream's logical cursor after direct handle I/O.
        stream.Position = position;

        return 0;
    }
}
