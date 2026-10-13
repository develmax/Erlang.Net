using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Erlang;

internal sealed class LinuxPortPipeStream(SafeFileHandle handle, bool readable) : Stream
{
    public override bool CanRead => readable;
    public override bool CanWrite => !readable;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (buffer.IsEmpty)
            return 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (int count, int error) = Transfer(buffer, true);
            if (count >= 0)
                return count;
            if (error == LinuxPortParameters.Interrupted)
                continue;
            if (error != LinuxPortParameters.WouldBlock)
                throw new NativePortIOException(error);
            await Task.Delay(LinuxPortParameters.PollDelayMilliseconds, cancellationToken);
        }
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        while (!buffer.IsEmpty)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (int count, int error) = Transfer(MemoryMarshal.AsMemory(buffer), false);
            if (count > 0)
            {
                buffer = buffer[count..];
                continue;
            }
            if (error == LinuxPortParameters.Interrupted)
                continue;
            if (error != LinuxPortParameters.WouldBlock)
                throw new NativePortIOException(error);
            await Task.Delay(LinuxPortParameters.PollDelayMilliseconds, cancellationToken);
        }
    }

    private unsafe (int Count, int Error) Transfer(Memory<byte> buffer, bool read)
    {
        bool added = false;
        try
        {
            if (handle.IsClosed)
                return (-1, LinuxPortParameters.BadDescriptor);
            handle.DangerousAddRef(ref added);
            using var pin = buffer.Pin();
            nint count = read
                ? LinuxPortNative.read(handle.DangerousGetHandle().ToInt32(), (nint)pin.Pointer, (nuint)buffer.Length)
                : LinuxPortNative.write(handle.DangerousGetHandle().ToInt32(), (nint)pin.Pointer, (nuint)buffer.Length);

            return ((int)count, count < 0 ? Marshal.GetLastPInvokeError() : 0);
        }
        finally
        {
            if (added)
                handle.DangerousRelease();
        }
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

    public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();
}
