using System.Buffers.Binary;
using System.Text;

internal static class BeamCorpusChunkComparison
{
    internal static string Describe(byte[] original, byte[] generated)
    {
        var expected = Chunks(original);
        var actual = Chunks(generated);
        var differences = new List<string>();
        foreach (string id in expected.Keys.Concat(actual.Keys).Distinct(StringComparer.Ordinal))
        {
            if (!expected.TryGetValue(id, out var reference))
            {
                differences.Add(id + " added");
            }
            else if (!actual.TryGetValue(id, out var output))
            {
                differences.Add(id + " missing");
            }
            else if (!reference.Span.SequenceEqual(output.Span))
            {
                differences.Add($"{id} {reference.Length}->{output.Length}");
            }
        }

        return differences.Count == 0 ? "chunk order or container bytes" : string.Join(", ", differences);
    }

    private static Dictionary<string, ReadOnlyMemory<byte>> Chunks(byte[] image)
    {
        var chunks = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);
        int offset = 12;
        while (image.Length - offset >= 8)
        {
            string id = Encoding.ASCII.GetString(image, offset, 4);
            int size = BinaryPrimitives.ReadInt32BigEndian(image.AsSpan(offset + 4, 4));
            offset += 8;
            if (size < 0 || size > image.Length - offset)
            {
                break;
            }
            chunks[id] = image.AsMemory(offset, size);
            offset += size;
            offset += (4 - size % 4) % 4;
        }

        return chunks;
    }
}
