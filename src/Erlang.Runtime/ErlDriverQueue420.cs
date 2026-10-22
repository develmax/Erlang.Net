using System.Runtime.InteropServices;

namespace Erlang;

// A dedicated queue/PDL gate avoids taking the port callback gate from a
// driver-created thread which already owns the port data lock.
internal sealed class ErlDriverQueue420 : IDisposable
{
    private readonly ErlDriverPrimitiveAccess420 memory;
    private readonly ErlDriverAbiLayout420 layout;
    private ErlDriverQueueSnapshot420 current;
    private bool closed;

    internal ErlDriverQueue420(ErlDriverPrimitiveAccess420 memory, ErlDriverAbiLayout420 layout)
    {
        this.memory = memory;
        this.layout = layout;
        current = new(memory, layout);
    }

    internal object Gate { get; } = new();

    internal nuint Size
    {
        get
        {
            lock (Gate)
            {
                RequireOpen();

                return current.Size;
            }
        }
    }

    private void RequireOpen() => ObjectDisposedException.ThrowIf(closed, this);

    private void CopyExisting(ErlDriverQueueSnapshot420 destination, nuint skip = 0)
    {
        foreach (var slice in current.Slices)
        {
            nuint dropped = nuint.Min(skip, slice.Size);
            skip -= dropped;
            destination.Add(slice.Binary, slice.Offset + dropped, slice.Size - dropped);
        }
    }

    private void Replace(Action<ErlDriverQueueSnapshot420> compose)
    {
        var staged = new ErlDriverQueueSnapshot420(memory, layout);
        try
        {
            compose(staged);
            staged.Build();
        }
        catch (Exception primary)
        {
            try
            {
                staged.Dispose();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(primary, cleanup);
            }
            throw;
        }
        var previous = current;
        current = staged;
        previous.Dispose();
    }

    internal void EnqueueBytes(nint source, nuint size, bool prepend)
    {
        if (size == 0) return;
        nint binary = memory.CopyToBinary(source, size);
        try
        {
            EnqueueBinary(
                binary,
                0,
                size,
                prepend
            );
        }
        finally
        {
            memory.ReleaseBinary(binary);
        }
    }

    internal void EnqueueBinary(
        nint binary,
        nuint offset,
        nuint size,
        bool prepend
    )
    {
        if (size == 0) return;
        lock (Gate)
        {
            RequireOpen();
            memory.ValidateSlice(binary, offset, size);
            Replace(next =>
            {
                if (prepend) next.Add(binary, offset, size);
                CopyExisting(next);
                if (!prepend) next.Add(binary, offset, size);
            });
        }
    }

    private void CopyVector(ErlDriverQueueSnapshot420 destination, nint vector, nuint skip)
    {
        if (vector == nint.Zero) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        int count = Marshal.ReadInt32(vector, layout.Field(ErlDriverMemoryNames420.IoVecCount));
        nuint size = unchecked((nuint)Marshal.ReadInt64(vector, layout.Field(ErlDriverMemoryNames420.IoVecSize)));
        nint entries = Marshal.ReadIntPtr(vector, layout.Field(ErlDriverMemoryNames420.IoVecVectors));
        nint binaries = Marshal.ReadIntPtr(vector, layout.Field(ErlDriverMemoryNames420.IoVecBinaries));
        if (count < 0 || skip > size || count != 0 && entries == nint.Zero)
            throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        nuint total = 0;
        int stride = layout.Field(ErlDriverMemoryNames420.SystemVectorBytes);
        for (int index = 0; index < count; index++)
        {
            nint entry = checked(entries + checked(index * stride));
            nuint length = unchecked((uint)Marshal.ReadInt32(entry, layout.Field(ErlDriverMemoryNames420.SystemVectorLength)));
            nint bytes = Marshal.ReadIntPtr(entry, layout.Field(ErlDriverMemoryNames420.SystemVectorAddress));
            total = checked(total + length);
            nuint dropped = nuint.Min(skip, length);
            skip -= dropped;
            length -= dropped;
            if (length == 0) continue;
            bytes = checked(bytes + checked((nint)dropped));
            nint binary = binaries == nint.Zero ? nint.Zero : Marshal.ReadIntPtr(binaries, checked(index * IntPtr.Size));
            if (binary == nint.Zero)
            {
                nint copied = memory.CopyToBinary(bytes, length);
                try
                {
                    destination.Add(copied, 0, length);
                }
                finally
                {
                    memory.ReleaseBinary(copied);
                }
            }
            else
            {
                nint start = memory.BinaryBytes(binary);
                if (bytes < start) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                destination.Add(binary, checked((nuint)(bytes - start)), length);
            }
        }
        if (skip != 0 || total != size) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
    }

    internal void EnqueueVector(nint vector, nuint skip, bool prepend)
    {
        lock (Gate)
        {
            RequireOpen();
            Replace(next =>
            {
                if (prepend) CopyVector(next, vector, skip);
                CopyExisting(next);
                if (!prepend) CopyVector(next, vector, skip);
            });
        }
    }

    internal nuint Dequeue(nuint size)
    {
        lock (Gate)
        {
            RequireOpen();
            if (size > current.Size) return nuint.MaxValue;
            if (size != 0) Replace(next => CopyExisting(next, size));

            return current.Size;
        }
    }

    internal nint Peek(nint count)
    {
        lock (Gate)
        {
            RequireOpen();
            if (count == nint.Zero) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
            Marshal.WriteInt32(count, current.Count);

            return current.Vectors;
        }
    }

    internal nuint PeekVector(nint vector)
    {
        lock (Gate)
        {
            RequireOpen();
            current.WriteVector(vector);

            return current.Size;
        }
    }

    public void Dispose()
    {
        lock (Gate)
        {
            if (closed) return;
            closed = true;
            current.Dispose();
        }
    }
}
