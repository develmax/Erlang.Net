using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Erlang;

internal sealed unsafe class PersistentTable416 : SafeHandle
{
    private readonly object gate = new();
    // Only owner/lease metadata: no keys, values, hashes or bucket occupancy here.
    private PersistentPairArena416[]? roots;
    private bool retired;
    private readonly PersistentArenaAccounting416 accounting;
    private long allocated;
    internal int RequestedBytes { get; }
    internal long AllocatedBytes => Interlocked.Read(ref allocated);
    public override bool IsInvalid => handle == 0;

    private PersistentTable416(byte[] bytes, PersistentPairArena416[] roots, PersistentArenaAccounting416 accounting) : base(0, true)
    {
        this.accounting = accounting;
        this.roots = roots;
        RequestedBytes = bytes.Length;
        SetHandle(accounting.Allocate(PersistentAllocationKind416.Table, RequestedBytes));
        try
        {
            Marshal.Copy(
                bytes,
                0,
                handle,
                bytes.Length
            );
            allocated = bytes.Length;
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    internal static PersistentTable416 Create(
        int capacity,
        IEnumerable<PersistentTableEntry416> entries,
        bool preserveSlots = false,
        PersistentArenaAccounting416? accounting = null
    )
    {
        PersistentTableEntry416[] pending = entries.ToArray();
        if (capacity < PersistentTableNames416.InitialCapacity || (capacity & (capacity - 1)) != 0
            || pending.Length >= capacity) throw new InvalidDataException(PersistentTableNames416.Invalid);
        var bytes = new byte[checked(PersistentTableNames416.HeaderBytes + capacity * PersistentTableNames416.SlotBytes)];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, PersistentTableNames416.Magic);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(PersistentTableNames416.ExtentOffset), bytes.LongLength);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(PersistentTableNames416.CapacityOffset), capacity);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(PersistentTableNames416.CountOffset), pending.Length);
        var owners = new PersistentPairArena416[pending.Length];
        var unique = new HashSet<PersistentPairArena416>(ReferenceEqualityComparer.Instance);
        long active = 0;
        for (int index = 0; index < pending.Length; index++)
        {
            PersistentTableEntry416 entry = pending[index];
            if (!unique.Add(entry.Area)) throw new InvalidDataException(PersistentTableNames416.Invalid);
            owners[index] = entry.Area;
            active = checked(active + entry.Area.RequestedBytes);
            int slot = preserveSlots && entry.Slot >= 0 ? entry.Slot : (int)(entry.Hash & (uint)(capacity - 1));
            if ((uint)slot >= (uint)capacity) throw new InvalidDataException(PersistentTableNames416.Invalid);
            if (preserveSlots && entry.Slot >= 0 && RootIndex(bytes, slot) != 0)
                throw new InvalidDataException(PersistentTableNames416.Invalid);
            while (RootIndex(bytes, slot) != 0) slot = (slot + 1) & (capacity - 1);
            int offset = PersistentTableNames416.HeaderBytes + slot * PersistentTableNames416.SlotBytes;
            BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(offset), entry.Hash);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset + sizeof(ulong)), index + 1);
        }
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(PersistentTableNames416.ActivePairBytesOffset), active);

        return new(bytes, owners, accounting ?? new());
    }

    internal PersistentArenaInfo416 Info()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(retired, this);
            var bytes = new byte[PersistentTableNames416.HeaderBytes];
            Marshal.Copy(
                handle,
                bytes,
                0,
                bytes.Length
            );
            long extent = BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(PersistentTableNames416.ExtentOffset));

            return new(
                BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(PersistentTableNames416.CountOffset)),
                checked(extent + BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(PersistentTableNames416.ActivePairBytesOffset))),
                BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(PersistentTableNames416.CapacityOffset)),
                extent,
                BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(PersistentTableNames416.ActivePairBytesOffset))
            );
        }
    }

    internal PersistentTableEntry416[] Entries()
    {
        lock (gate)
        {
            byte[] bytes = Copy();
            var current = roots!;
            var result = new List<PersistentTableEntry416>(current.Length);
            int capacity = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(PersistentTableNames416.CapacityOffset));
            for (int slot = 0; slot < capacity; slot++)
            {
                int root = RootIndex(bytes, slot);
                if (root != 0)
                {
                    if ((uint)(root - 1) >= (uint)current.Length) throw new InvalidDataException(PersistentTableNames416.Invalid);
                    int offset = PersistentTableNames416.HeaderBytes + slot * PersistentTableNames416.SlotBytes;
                    result.Add(new(BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(offset)), current[root - 1], slot));
                }
            }

            return result.ToArray();
        }
    }

    // Caller serializes acquisition against pair retirement with its store gate.
    internal PersistentTableRead416 AcquireRead()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(retired, this);
            bool retained = false;
            DangerousAddRef(ref retained);
            PersistentArenaRead416?[]? leases = null;
            try
            {
                var current = roots!;
                leases = new PersistentArenaRead416?[current.Length];
                for (int index = 0; index < current.Length; index++) leases[index] = current[index].AcquireRead();

                return new(this, Copy(), leases);
            }
            catch
            {
                if (leases is not null) foreach (var lease in leases) lease?.Dispose();
                if (retained) DangerousRelease();
                throw;
            }
        }
    }

    internal IReadOnlyList<PersistentPairArena416> AreaOwnersForRetirement()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(retired, this);

            return roots!;
        }
    }

    internal static int RootIndex(byte[] image, int slot)
        => BinaryPrimitives.ReadInt32LittleEndian(image.AsSpan(
            checked(PersistentTableNames416.HeaderBytes + slot * PersistentTableNames416.SlotBytes + sizeof(ulong))));

    private byte[] Copy()
    {
        ObjectDisposedException.ThrowIf(retired, this);
        var bytes = new byte[RequestedBytes];
        Marshal.Copy(
            handle,
            bytes,
            0,
            bytes.Length
        );

        return bytes;
    }

    internal void Retire()
    {
        lock (gate)
        {
            if (retired) return;
            retired = true;
            Dispose();
        }
    }

    protected override bool ReleaseHandle()
    {
        // Store publication, not a table-version destructor, owns pair retirement.
        Interlocked.Exchange(ref roots, null);
        try
        {
            accounting.Free(PersistentAllocationKind416.Table, handle, RequestedBytes);
        }
        catch (Exception failure)
        {
            accounting.ReportReleaseFailure(failure);
        }
        Interlocked.Exchange(ref allocated, 0);

        return true;
    }
}
