using System.IO.Compression;
namespace Erlang;

internal static class EtsCompressedReader438
{
    internal static RuntimeEtsStoredNode[] Decode(RuntimeEtsCompressed438 state, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (state.DecodedBytes < sizeof(int) * 4 || state.NodeCount < 1 || state.DecodedBytes > Array.MaxLength) throw Invalid();
        byte[] plain = new byte[state.DecodedBytes];
        using (var packed = new MemoryStream(state.Encoded, false))
        using (var decoder = new BrotliStream(packed, CompressionMode.Decompress))
        {
            int offset = 0;
            while (offset < plain.Length)
            {
                cancellation.ThrowIfCancellationRequested();
                int count = decoder.Read(plain, offset, Math.Min(EtsCompressedNames438.Chunk, plain.Length - offset));
                if (count == 0) throw Invalid();
                offset += count;
                EtsCodecEvents442.Log.DecodeProgress(offset, plain.Length);
                cancellation.ThrowIfCancellationRequested();
            }
            if (decoder.ReadByte() != -1) throw Invalid();
        }
        using var stream = new MemoryStream(plain, false);
        using var reader = new BinaryReader(stream);
        if (reader.ReadInt32() != EtsCompressedNames438.Magic || reader.ReadInt32() != EtsCompressedNames438.Version
            || reader.ReadInt32() != state.NodeCount) throw Invalid();
        int encodedCount = reader.ReadInt32();
        if (encodedCount < 0 || encodedCount > (stream.Length - stream.Position) / sizeof(int)
            || encodedCount != state.NodeCount - state.KeyNodes.Length || state.KeyOriginalIds.Length != state.KeyNodes.Length) throw Invalid();
        var result = new RuntimeEtsStoredNode[state.NodeCount];
        var assigned = new bool[state.NodeCount];
        for (int i = 0; i < state.KeyNodes.Length; i++)
        {
            cancellation.ThrowIfCancellationRequested();
            int id = state.KeyOriginalIds[i];
            if ((uint)id >= (uint)result.Length || assigned[id]) throw Invalid();
            var node = state.KeyNodes[i];
            result[id] = node with
            {
                Children = node.Children?.Select(child =>
            {
                cancellation.ThrowIfCancellationRequested();

                return (uint)child < (uint)state.KeyOriginalIds.Length ? state.KeyOriginalIds[child] : throw Invalid();
            }).ToArray()
            };
            assigned[id] = true;
        }
        for (int i = 0; i < encodedCount; i++)
        {
            cancellation.ThrowIfCancellationRequested();
            int id = reader.ReadInt32();
            if ((uint)id >= (uint)result.Length || assigned[id]) throw Invalid();
            RuntimeEtsStoredKind kind = (RuntimeEtsStoredKind)reader.ReadByte();
            int length = Length(reader, 1);
            byte[]? bytes = length < 0 ? null : ReadBytes(reader, length, cancellation);
            int bits = reader.ReadInt32();
            long floating = reader.ReadInt64();
            length = Length(reader, sizeof(int));
            int[]? children = length < 0 ? null : new int[length];
            if (children is not null) for (int c = 0; c < children.Length; c++)
            {
                cancellation.ThrowIfCancellationRequested();
                children[c] = reader.ReadInt32();
                if ((uint)children[c] >= (uint)result.Length) throw Invalid();
            }
            length = Length(reader, sizeof(int));
            string[]? names = length < 0 ? null : new string[length];
            if (names is not null) for (int n = 0; n < names.Length; n++)
            {
                int chars = Length(reader, sizeof(ushort));
                if (chars < 0) throw Invalid();
                var text = new char[chars];
                for (int c = 0; c < chars; c++)
                {
                    if ((c & (EtsCompressedNames438.Chunk - 1)) == 0) cancellation.ThrowIfCancellationRequested();
                    text[c] = (char)reader.ReadUInt16();
                }
                names[n] = new string(text);
            }
            bool exported = reader.ReadBoolean();
            int identity = reader.ReadInt32(), function = reader.ReadInt32();
            if (identity < -1 || identity >= state.Identities.Length || function < -1 || function >= state.Functions.Length) throw Invalid();
            var node = new RuntimeEtsStoredNode(
                kind,
                bytes,
                bits,
                floating,
                children,
                names,
                exported,
                identity < 0 ? null : state.Identities[identity],
                function < 0 ? null : state.Functions[function]
            );
            Validate(node);
            result[id] = node;
            assigned[id] = true;
        }
        if (stream.Position != stream.Length || assigned.Any(value => !value)) throw Invalid();
        ValidateGraph(result, cancellation);

        return result;
    }

    private static int Length(BinaryReader reader, int unit)
    {
        int count = reader.ReadInt32();
        if (count < -1 || count > (reader.BaseStream.Length - reader.BaseStream.Position) / unit) throw Invalid();

        return count;
    }

    private static byte[] ReadBytes(BinaryReader reader, int count, CancellationToken cancellation)
    {
        byte[] bytes = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            cancellation.ThrowIfCancellationRequested();
            int size = Math.Min(EtsCompressedNames438.Chunk, count - offset);
            reader.BaseStream.ReadExactly(bytes.AsSpan(offset, size));
            offset += size;
        }

        return bytes;
    }

    private static void Validate(RuntimeEtsStoredNode n)
    {
        bool valid = n.Kind switch
        {
            RuntimeEtsStoredKind.Nil or RuntimeEtsStoredKind.Float => n.Children is null,
            RuntimeEtsStoredKind.Atom => n.Names?.Length == 1,
            RuntimeEtsStoredKind.Integer => n.Bytes is { Length: > 0 },
            RuntimeEtsStoredKind.Binary => n.Bytes is not null && n.BitLength >= 0 && n.BitLength <= (long)n.Bytes.Length * 8,
            RuntimeEtsStoredKind.Cons => n.Children?.Length == 2,
            RuntimeEtsStoredKind.Tuple => n.Children is not null,
            RuntimeEtsStoredKind.Map => n.Children is not null && n.Children.Length % 2 == 0,
            RuntimeEtsStoredKind.NativeRecord => n.Children is not null && n.Names?.Length == n.Children.Length + 2,
            RuntimeEtsStoredKind.Function => n.Children is not null && n.Function is not null,
            RuntimeEtsStoredKind.Identity => n.Identity is not null,
            _ => false
        };
        if (!valid) throw Invalid();
    }

    private static void ValidateGraph(RuntimeEtsStoredNode[] nodes, CancellationToken cancellation)
    {
        var colors = new byte[nodes.Length];
        var pending = new Stack<(int Id, bool Finish)>();
        pending.Push((0, false));
        int count = 0;
        while (pending.TryPop(out var next))
        {
            cancellation.ThrowIfCancellationRequested();
            if (next.Finish)
            {
                colors[next.Id] = 2;
                continue;
            }
            if (colors[next.Id] == 2) continue;
            if (colors[next.Id] == 1) throw Invalid();
            colors[next.Id] = 1;
            count++;
            pending.Push((next.Id, true));
            if (nodes[next.Id].Children is { } children) foreach (int child in children.Reverse())
            {
                cancellation.ThrowIfCancellationRequested();
                pending.Push((child, false));
            }
        }
        if (count != nodes.Length) throw Invalid();
    }

    private static InvalidDataException Invalid() => new(EtsCompressedNames438.Invalid);
}
