using System.Buffers.Binary;
using static Erlang.PersistentArenaSelection591Names;

namespace Erlang;

// Offset reachability only. No root/materialization/admission callbacks here.
internal static class PersistentArenaSelection591
{
    internal static HashSet<ulong> Escaping(
        byte[] bytes,
        ulong root,
        bool valueOnly,
        CancellationToken cancellation
    )
    {
        ulong selected = root;
        if (valueOnly)
        {
            int position = Position(bytes, root);
            int length = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(position + LengthOffset));
            int children = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(position + ChildrenOffset));
            Require(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(position + KindOffset)) == (int)PersistentArenaKind416.Tuple
                && length == 0 && children == PairArity
                && BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(position + ExternalRootOffset)) == PersistentArenaNames416.MissingRoot);
            Require((long)position + PersistentArenaNames416.NodeHeaderBytes
                + (long)children * PersistentArenaNames416.OffsetBytes <= bytes.Length);
            selected = BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(position
                + PersistentArenaNames416.NodeHeaderBytes + ValueIndex * PersistentArenaNames416.OffsetBytes));
            Require(selected < root);
        }
        var escaped = new HashSet<ulong>();
        var pending = new Stack<ulong>();
        pending.Push(selected);
        while (pending.TryPop(out ulong offset))
        {
            cancellation.ThrowIfCancellationRequested();
            if (!escaped.Add(offset))
                continue;
            int position = Position(bytes, offset);
            int length = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(position + LengthOffset));
            int children = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(position + ChildrenOffset));
            Require(length >= 0 && children >= 0
                && (long)position + PersistentArenaNames416.NodeHeaderBytes + length
                    + (long)children * PersistentArenaNames416.OffsetBytes <= bytes.Length);
            int childStart = checked(position + PersistentArenaNames416.NodeHeaderBytes + length);
            for (int index = 0; index < children; index++)
            {
                cancellation.ThrowIfCancellationRequested();
                ulong child = BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(childStart + index * PersistentArenaNames416.OffsetBytes));
                Require(child < offset);
                pending.Push(child);
            }
        }

        return escaped;
    }

    private static int Position(byte[] bytes, ulong offset)
    {
        Require(bytes.Length >= PersistentArenaNames416.HeaderBytes
            && offset >= PersistentArenaNames416.HeaderBytes
            && offset <= (ulong)(bytes.Length - PersistentArenaNames416.NodeHeaderBytes));

        return (int)offset;
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidDataException(PersistentArenaNames416.Invalid);
    }
}
