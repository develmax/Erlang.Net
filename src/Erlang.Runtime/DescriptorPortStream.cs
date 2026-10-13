using System.Runtime.InteropServices;

namespace Erlang;

internal sealed class DescriptorPortStream(int descriptor, bool readable) : Stream
{
    private bool disposed;
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
            await Ready(DescriptorPortParameters.PollInput, cancellationToken);
            (int count, int error) = Transfer(buffer, true);
            if (count >= 0)
                return count;
            if (error != LinuxPortParameters.Interrupted && error != LinuxPortParameters.WouldBlock)
                throw new NativePortIOException(error);
        }
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        while (!buffer.IsEmpty)
        {
            await Ready(DescriptorPortParameters.PollOutput, cancellationToken);
            // Blocking descriptors use a worker, just as the original fd driver uses async writes.
            var remaining = buffer;
            (int count, int error) = await Task.Run(() => Transfer(MemoryMarshal.AsMemory(remaining), false), cancellationToken);
            if (count > 0)
                buffer = buffer[count..];
            else if (error != LinuxPortParameters.Interrupted && error != LinuxPortParameters.WouldBlock)
                throw new NativePortIOException(error);
        }
    }

    private async Task Ready(short events, CancellationToken cancellation)
    {
        while (true)
        {
            cancellation.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(disposed, this);
            var state = new DescriptorPollState { Descriptor = descriptor, Events = events };
            int result = poll(ref state, 1, 0);
            if (result > 0)
            {
                if ((state.Returned & DescriptorPortParameters.PollInvalid) != 0)
                    throw new NativePortIOException(LinuxPortParameters.BadDescriptor);

                return;
            }
            if (result < 0 && Marshal.GetLastPInvokeError() != LinuxPortParameters.Interrupted)
                throw new NativePortIOException(Marshal.GetLastPInvokeError());
            await Task.Delay(DescriptorPortParameters.PollIntervalMilliseconds, cancellation);
        }
    }

    private unsafe (int Count, int Error) Transfer(Memory<byte> buffer, bool read)
    {
        using var pin = buffer.Pin();
        nint count = read
            ? LinuxPortNative.read(descriptor, (nint)pin.Pointer, (nuint)buffer.Length)
            : LinuxPortNative.write(descriptor, (nint)pin.Pointer, (nuint)buffer.Length);

        return ((int)count, count < 0 ? Marshal.GetLastPInvokeError() : 0);
    }

    protected override void Dispose(bool disposing)
    {
        disposed = true;
        base.Dispose(disposing);
    }

    public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

    public override void Write(byte[] buffer, int offset, int count) => WriteAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

    public override void Flush() { }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    [DllImport(LinuxPortParameters.Library, SetLastError = true)]
    private static extern int poll(ref DescriptorPollState descriptors, nuint count, int timeout);
}
