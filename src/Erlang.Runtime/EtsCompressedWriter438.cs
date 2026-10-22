using System.IO.Compression;
namespace Erlang;

internal static class EtsCompressedWriter438
{
    internal static RuntimeEtsCompressed438 Encode(RuntimeEtsStoredNode[] nodes, long keyPosition, CancellationToken cancellation)
    {
        if (nodes.Length == 0 || nodes[0].Kind != RuntimeEtsStoredKind.Tuple || nodes[0].Children is not { } fields
            || keyPosition < 1 || keyPosition > fields.Length) throw Invalid();
        int keyRoot = fields[(int)keyPosition - 1];
        var keyMap = new Dictionary<int, int>();
        var keyIds = new List<int>();
        var pending = new Stack<int>();
        pending.Push(keyRoot);
        while (pending.TryPop(out int index))
        {
            cancellation.ThrowIfCancellationRequested();
            if (keyMap.ContainsKey(index)) continue;
            keyMap.Add(index, keyIds.Count);
            keyIds.Add(index);
            if (nodes[index].Children is { } children)
                foreach (int child in children)
                {
                    cancellation.ThrowIfCancellationRequested();
                    pending.Push(child);
                }
        }
        var keyNodes = new RuntimeEtsStoredNode[keyIds.Count];
        for (int i = 0; i < keyIds.Count; i++)
        {
            cancellation.ThrowIfCancellationRequested();
            var node = nodes[keyIds[i]];
            keyNodes[i] = node with
            {
                Children = node.Children?.Select(child =>
 {
     cancellation.ThrowIfCancellationRequested();

     return keyMap[child];
 }).ToArray()
            };
        }
        var identities = new List<RuntimeExternalTermReservation>();
        var functions = new List<IRuntimeStoredFunctionRecipe>();
        using var plain = new MemoryStream();
        using (var writer = new BinaryWriter(plain, System.Text.Encoding.UTF8, true))
        {
            writer.Write(EtsCompressedNames438.Magic);
            writer.Write(EtsCompressedNames438.Version);
            writer.Write(nodes.Length);
            writer.Write(nodes.Length - keyNodes.Length);
            for (int index = 0; index < nodes.Length; index++)
            {
                cancellation.ThrowIfCancellationRequested();
                if (keyMap.ContainsKey(index)) continue;
                var node = nodes[index];
                writer.Write(index);
                writer.Write((byte)node.Kind);
                WriteBytes(writer, node.Bytes, cancellation);
                writer.Write(node.BitLength);
                writer.Write(node.FloatBits);
                writer.Write(node.Children?.Length ?? EtsCompressedNames438.NullLength);
                if (node.Children is { } children)
                    foreach (int child in children)
                    {
                        cancellation.ThrowIfCancellationRequested();
                        writer.Write(child);
                    }
                writer.Write(node.Names?.Length ?? EtsCompressedNames438.NullLength);
                if (node.Names is { } names)
                    foreach (string name in names)
                    {
                        cancellation.ThrowIfCancellationRequested();
                        writer.Write(name.Length);
                        // Atom strings can contain raw UTF16 code units: never replace with lossy UTF8.
                        for (int c = 0; c < name.Length; c++)
                        {
                            if ((c & (EtsCompressedNames438.Chunk - 1)) == 0) cancellation.ThrowIfCancellationRequested();
                            writer.Write((ushort)name[c]);
                        }
                    }
                writer.Write(node.Exported);
                writer.Write(node.Identity is null ? EtsCompressedNames438.NullLength : identities.Count);
                if (node.Identity is not null) identities.Add(node.Identity);
                writer.Write(node.Function is null ? EtsCompressedNames438.NullLength : functions.Count);
                if (node.Function is not null) functions.Add(node.Function);
            }
        }
        int decodedBytes = checked((int)plain.Length);
        using var packed = new MemoryStream();
        plain.Position = 0;
        using (var compressor = new BrotliStream(packed, CompressionLevel.Optimal, true))
        {
            byte[] buffer = new byte[EtsCompressedNames438.Chunk];
            int count;
            long completed = 0;
            while ((count = plain.Read(buffer)) != 0)
            {
                cancellation.ThrowIfCancellationRequested();
                compressor.Write(buffer, 0, count);
                completed += count;
                EtsCodecEvents442.Log.EncodeProgress(completed, decodedBytes);
                cancellation.ThrowIfCancellationRequested();
            }
        }
        cancellation.ThrowIfCancellationRequested();

        return new(
            packed.ToArray(),
            decodedBytes,
            nodes.Length,
            keyPosition,
            keyIds.ToArray(),
            keyNodes,
            identities.ToArray(),
            functions.ToArray()
        );
    }

    private static void WriteBytes(BinaryWriter writer, byte[]? bytes, CancellationToken cancellation)
    {
        writer.Write(bytes?.Length ?? EtsCompressedNames438.NullLength);
        if (bytes is null) return;
        for (int offset = 0; offset < bytes.Length;)
        {
            cancellation.ThrowIfCancellationRequested();
            int count = Math.Min(EtsCompressedNames438.Chunk, bytes.Length - offset);
            writer.Write(bytes, offset, count);
            offset += count;
        }
    }

    private static InvalidDataException Invalid() => new(EtsCompressedNames438.Invalid);
}
