using System.Buffers.Binary;

namespace Erlang.Tests;

internal static class OrdsetsEncodedCorpus
{
    private const byte Version = 131;
    private const byte List = 108;
    private const byte Nil = 106;
    private const int HeaderBytes = 6;
    private const int CountOffset = 2;

    internal static byte[] Combine(IReadOnlyList<byte[]> batches, int expectedCount)
    {
        using var output = new MemoryStream();
        byte[] header = new byte[HeaderBytes];
        header[0] = Version;
        header[1] = List;
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(CountOffset), checked((uint)expectedCount));
        output.Write(header);
        int count = 0;
        // observe/2 reverses its input. Reverse the batch order, retaining every
        // actual encoded row byte untouched, to reproduce its whole-list order.
        for (int index = batches.Count - 1; index >= 0; index--)
        {
            byte[] bytes = batches[index];
            if (bytes.Length <= HeaderBytes || bytes[0] != Version || bytes[1] != List || bytes[^1] != Nil)
                throw new InvalidDataException(OrdsetsCompatibilityNames.Failure);
            count = checked(count + (int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(CountOffset)));
            output.Write(bytes.AsSpan(HeaderBytes, bytes.Length - HeaderBytes - 1));
        }
        if (count != expectedCount)
            throw new InvalidDataException(OrdsetsCompatibilityNames.Failure);
        output.WriteByte(Nil);

        return output.ToArray();
    }
}
