using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Erlang;

internal sealed class WindowsDescriptorPortStream(SafeFileHandle handle, bool readable) : Stream
{
    public override bool CanRead => readable;
    public override bool CanWrite => !readable;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        new(Task.Run(() => Transfer(buffer, true, cancellationToken), cancellationToken));

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        while (!buffer.IsEmpty)
        {
            var remaining = buffer;
            int count = await Task.Run(() => Transfer(MemoryMarshal.AsMemory(remaining), false, cancellationToken), cancellationToken);
            buffer = buffer[count..];
        }
    }

    private unsafe int Transfer(Memory<byte> buffer, bool read, CancellationToken cancellation)
    {
        if (buffer.IsEmpty)
            return 0;
        ObjectDisposedException.ThrowIf(handle.IsClosed, this);
        using SafeFileHandle thread = OpenThread(DescriptorPortParameters.ThreadCancelAccess, false, GetCurrentThreadId());
        if (thread.IsInvalid)
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        void Cancel()
        {
            try
            {
                CancelSynchronousIo(thread);
            }
            catch (ObjectDisposedException) { }
        }
        using var registration = cancellation.Register(Cancel);
        using var retries = cancellation.CanBeCanceled ? new Timer(
            _ =>
        {
            if (cancellation.IsCancellationRequested)
                Cancel();
        },
            null,
            DescriptorPortParameters.PollIntervalMilliseconds,
            DescriptorPortParameters.PollIntervalMilliseconds
        ) : null;
        cancellation.ThrowIfCancellationRequested();
        using var pin = buffer.Pin();
        bool success = read
            ? ReadFile(
                handle,
                (nint)pin.Pointer,
                (uint)buffer.Length,
                out uint count,
                nint.Zero
            )
            : WriteFile(
                handle,
                (nint)pin.Pointer,
                (uint)buffer.Length,
                out count,
                nint.Zero
            );
        if (success)
        {
            if (!read)
                return (int)count;
            int written = 0;
            var span = buffer.Span[..(int)count];
            for (int index = 0; index < span.Length; index++)
            {
                byte value = span[index];
                if (value == (byte)'\r')
                {
                    if (index + 1 < span.Length && span[index + 1] == (byte)'\n')
                        continue;
                    value = (byte)'\n';
                }
                span[written++] = value;
            }

            return written;
        }
        int error = Marshal.GetLastPInvokeError();
        if (error == DescriptorPortParameters.WindowsOperationAborted)
            cancellation.ThrowIfCancellationRequested();
        if (read && error == DescriptorPortParameters.WindowsBrokenPipe)
            return 0;

        throw new IOException(new Win32Exception(error).Message, error);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            handle.Dispose();
        base.Dispose(disposing);
    }

    public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

    public override void Write(byte[] buffer, int offset, int count) => WriteAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

    public override void Flush() { }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    [DllImport(WindowsPortParameters.Library, SetLastError = true)]
    private static extern SafeFileHandle OpenThread(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint id);

    [DllImport(WindowsPortParameters.Library)]
    private static extern uint GetCurrentThreadId();

    [DllImport(WindowsPortParameters.Library, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CancelSynchronousIo(SafeFileHandle thread);

    [DllImport(WindowsPortParameters.Library, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadFile(
        SafeFileHandle handle,
        nint buffer,
        uint bytes,
        out uint read,
        nint overlapped
    );

    [DllImport(WindowsPortParameters.Library, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WriteFile(
        SafeFileHandle handle,
        nint buffer,
        uint bytes,
        out uint written,
        nint overlapped
    );
}
