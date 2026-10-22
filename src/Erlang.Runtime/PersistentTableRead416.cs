using System.Buffers.Binary;

namespace Erlang;

internal sealed class PersistentTableRead416(
    PersistentTable416 table,
    byte[] snapshot,
    PersistentArenaRead416?[] leases
) : IDisposable
{
    private readonly object gate = new();
    private PersistentTable416? owned = table;
    private byte[]? image = snapshot;
    private PersistentArenaRead416?[]? areas = leases;

    internal TupleTerm? Find(Term key, ulong hash, CancellationToken cancellation)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(owned is null, this);
            byte[] current = image!;
            int capacity = BinaryPrimitives.ReadInt32LittleEndian(current.AsSpan(PersistentTableNames416.CapacityOffset));
            int slot = (int)(hash & (uint)(capacity - 1));
            for (int examined = 0; examined < capacity; examined++)
            {
                cancellation.ThrowIfCancellationRequested();
                int root = PersistentTable416.RootIndex(current, slot);
                if (root == 0) return null;
                int offset = PersistentTableNames416.HeaderBytes + slot * PersistentTableNames416.SlotBytes;
                if (BinaryPrimitives.ReadUInt64LittleEndian(current.AsSpan(offset)) == hash)
                {
                    TupleTerm pair = Area(root).Materialize(cancellation);
                    if (pair.Items[0].Equals(key)) return pair;
                }
                slot = (slot + 1) & (capacity - 1);
            }
            throw new InvalidDataException(PersistentTableNames416.Invalid);
        }
    }

    internal TupleTerm[] Pairs(CancellationToken cancellation)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(owned is null, this);
            byte[] current = image!;
            int capacity = BinaryPrimitives.ReadInt32LittleEndian(current.AsSpan(PersistentTableNames416.CapacityOffset));
            var result = new List<TupleTerm>();
            for (int slot = 0; slot < capacity; slot++)
            {
                cancellation.ThrowIfCancellationRequested();
                int root = PersistentTable416.RootIndex(current, slot);
                if (root != 0) result.Add(Area(root).Materialize(cancellation));
            }

            return result.ToArray();
        }
    }

    private PersistentArenaRead416 Area(int root)
    {
        if ((uint)(root - 1) >= (uint)areas!.Length || areas[root - 1] is not { } lease)
            throw new InvalidDataException(PersistentTableNames416.Invalid);

        return lease;
    }

    public void Dispose()
    {
        lock (gate)
        {
            var old = owned;
            if (old is null) return;
            owned = null;
            image = null;
            var retired = areas!;
            areas = null;
            try
            {
                foreach (var lease in retired) lease?.Dispose();
            }
            finally
            {
                old.DangerousRelease();
            }
        }
    }
}
