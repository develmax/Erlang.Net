using System.Runtime.InteropServices;

namespace Erlang;

// Source-only prerequisite candidate. Not activated until process-header,
// graph-custody, runtime-drain and Host-export integration is reviewed together.
internal sealed unsafe partial class OrdinaryBinaryBacking : INativeWritableBinaryStorage, IDisposable
{
    private readonly object gate = new();
    private readonly ProcessRuntime runtime;
    private int capacity;
    private readonly OrdinaryBinaryAllocationTicket ticket;
    private nint allocation;
    private int producerClosed;
    private readonly TaskCompletionSource retired = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal Task Retired => retired.Task;
    internal ProcessRuntime Runtime => runtime;

    private OrdinaryBinaryBacking(
        ProcessRuntime runtime,
        int capacity,
        nint allocation,
        OrdinaryBinaryAllocationTicket ticket
    )
    {
        this.ticket = ticket;
        this.runtime = runtime;
        this.capacity = capacity;
        this.allocation = allocation;
    }

    internal static OrdinaryBinaryBacking Capture(ProcessRuntime runtime, BinaryBacking source, OrdinaryBinaryAllocationTicket ticket)
    {
        nint selected = 0;
        try
        {
            if (IntPtr.Size != 8 || sizeof(OrdinaryBinaryHeader) != 32)
                throw new PlatformNotSupportedException(OrdinaryBinaryNames.Platform);
            if (!source.CanCaptureImmutableOrdinary)
                throw new InvalidOperationException(OrdinaryBinaryNames.Prepared);
            // Admission excludes every actual writable-origin producer. Do not
            // pin, revoke a writer, shrink spare capacity or snapshot mutability.
            // Immutable backing bytes/capacity cannot change via Append.

            byte[] bytes = source.Buffer;
            nuint size = checked((nuint)sizeof(OrdinaryBinaryHeader) + (nuint)bytes.Length);
            selected = (nint)NativeMemory.AllocZeroed(size);
            if (selected == 0) throw new OutOfMemoryException();
            var header = (OrdinaryBinaryHeader*)selected;
            header->References = 1;
            header->OriginalSize = bytes.Length;
            bytes.AsSpan().CopyTo(new Span<byte>((byte*)selected + sizeof(OrdinaryBinaryHeader), bytes.Length));

            return new OrdinaryBinaryBacking(
                runtime,
                bytes.Length,
                selected,
                ticket
            );
        }
        catch
        {
            NativeMemory.Free((void*)selected);
            ticket.Complete();
            throw;
        }
    }

    internal nint BinaryPointer
    {
        get
        {
            lock (gate) return Require();
        }
    }

    internal OrdinaryBinaryLease Retain()
    {
        var lease = new OrdinaryBinaryLease(this);
        lock (gate)
        {
            var header = (OrdinaryBinaryHeader*)Require();
            header->References = checked(header->References + 1);
            lease.Admit();
        }

        return lease;
    }

    internal void Release()
    {
        nint freed = 0;
        lock (gate)
        {
            var header = (OrdinaryBinaryHeader*)Require();
            if (--header->References == 0)
            {
                freed = allocation;
                allocation = 0;
            }
        }
        if (freed != 0)
        {
            NativeMemory.Free((void*)freed);
            retired.TrySetResult();
            ticket.Complete();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref producerClosed, 1) == 0) Release();
    }

    private nint Require()
    {
        if (allocation == 0) throw new ObjectDisposedException(nameof(OrdinaryBinaryBacking));
        var header = (OrdinaryBinaryHeader*)allocation;
        if ((header->Flags & ~NativeWritableBinaryLayout.WritableActive) != 0
            || header->Flags != 0 && (header->Flags & NativeWritableBinaryLayout.Writable) == 0
            || header->ApparentSize > (nuint)capacity || header->OriginalSize != capacity || header->References <= 0)
            throw new InvalidOperationException(OrdinaryBinaryNames.Invalid);

        return allocation;
    }

    internal BitString ExportPortable(BitString source)
    {
        PinWritable();
        using var lease = Retain();
        ResourceBitsRead577 read = source.CaptureResourceBits577();
        byte[] bytes = ((INativeBinaryStorage524)this).Bytes524.ToArray();

        return BitString.FromPersistentBacking416(new BinaryBacking(bytes, true), read.BitOffset, read.BitLength);
    }

    object INativeBinaryStorage524.ResourceIdentity524 => this;
    nint INativeBinaryStorage524.Pointer524
    {
        get
        {
            lock (gate) return Require() + sizeof(OrdinaryBinaryHeader);
        }
    }
    int INativeBinaryStorage524.DataLength524 => capacity;
    long INativeBinaryStorage524.AllocatedBytes524 => capacity;
    long INativeBinaryStorage524.ReferencedBytes524 => capacity;
    ReadOnlySpan<byte> INativeBinaryStorage524.Bytes524
    {
        get
        {
            lock (gate)
            {
                var header = (OrdinaryBinaryHeader*)Require();
                ReadOnlySpan<byte> native = new((byte*)header + sizeof(OrdinaryBinaryHeader), capacity);

                // Internal aligned readers may outlive this lock. A writable
                // buffer can move; this consumed ephemeral read snapshot is
                // copied FROM actual native bytes, never the storage authority.
                return (header->Flags & NativeWritableBinaryLayout.Writable) != 0 ? native.ToArray() : native;
            }
        }
    }

    // Bytes524 snapshots a writable allocation while holding its owner gate.
    // This short copy-read lease owns lifetime only; it never exports a pointer.
    IDisposable INativeWritableBinaryStorage.RetainCopyRead() => Retain();

    IDisposable INativeBinaryStorage524.RetainRoot524()
    {
        PinWritable();

        return Retain();
    }
}
