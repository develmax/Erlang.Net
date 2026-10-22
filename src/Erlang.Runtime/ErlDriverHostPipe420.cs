using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Erlang;

// One dedicated reader and serialized writers; diagnostics use separate child
// stdout/stderr, never this actual pipe. Session owner must join its reader and
// fail every pending invocation on transport failure before releasing images.
internal sealed class ErlDriverHostPipe420 : IAsyncDisposable
{
    private static readonly JsonSerializerOptions Json = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
    private readonly Stream stream;
    private readonly string session;
    private readonly int maximumFrameBytes;
    private readonly SemaphoreSlim writer = new(1, 1);
    private int closed;

    internal ErlDriverHostPipe420(Stream stream, string session, int maximumFrameBytes)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrEmpty(session);
        if (!stream.CanRead || !stream.CanWrite || maximumFrameBytes < ErlDriverHostNames420.MinimumFrameBytes)
            throw new ArgumentException(ErlDriverHostNames420.InvalidFrame);
        this.stream = stream;
        this.session = session;
        this.maximumFrameBytes = maximumFrameBytes;
    }

    internal async ValueTask WriteAsync(ErlDriverHostFrame420 value, CancellationToken cancellation)
    {
        Validate(value);
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value, Json);
        if (bytes.Length > maximumFrameBytes)
            throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        byte[] length = new byte[ErlDriverHostNames420.LengthBytes];
        BinaryPrimitives.WriteInt32LittleEndian(length, bytes.Length);
        await writer.WaitAsync(cancellation).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref closed) != 0, ErlDriverHostNames420.Closed);
            try
            {
                await stream.WriteAsync(length, cancellation).ConfigureAwait(false);
                await stream.WriteAsync(bytes, cancellation).ConfigureAwait(false);
                await stream.FlushAsync(cancellation).ConfigureAwait(false);
            }
            catch (Exception primary)
            {
                // A canceled partial write cannot be reused as a different frame.
                try
                {
                    Close();
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(primary, cleanup);
                }
                throw;
            }
        }
        finally
        {
            writer.Release();
        }
    }

    internal async ValueTask<ErlDriverHostFrame420> ReadAsync(CancellationToken cancellation)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref closed) != 0, ErlDriverHostNames420.Closed);
        try
        {
            byte[] length = new byte[ErlDriverHostNames420.LengthBytes];
            await stream.ReadExactlyAsync(length, cancellation).ConfigureAwait(false);
            int count = BinaryPrimitives.ReadInt32LittleEndian(length);
            if (count <= 0 || count > maximumFrameBytes)
                throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
            byte[] bytes = new byte[count];
            await stream.ReadExactlyAsync(bytes, cancellation).ConfigureAwait(false);
            ErlDriverHostFrame420 value = JsonSerializer.Deserialize<ErlDriverHostFrame420>(bytes, Json)
                ?? throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
            Validate(value);

            return value;
        }
        catch (Exception primary)
        {
            try
            {
                Close();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(primary, cleanup);
            }
            throw;
        }
    }

    private void Validate(ErlDriverHostFrame420 value)
    {
        if (value.Version != ErlDriverHostNames420.Version || value.Session != session
            || !Enum.IsDefined(value.Kind) || !Enum.IsDefined(value.Operation))
            throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
    }

    private void Close()
    {
        if (Interlocked.Exchange(ref closed, 1) == 0) stream.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Close();

        return ValueTask.CompletedTask;
    }
}
