using System.Buffers.Binary;
using System.Text;

namespace Erlang.Tests;

internal static class BeamfileMd5Host423Bytes
{
    internal static byte[] Words(params int[] words)
    {
        byte[] output = new byte[words.Length * sizeof(int)];
        for (int index = 0; index < words.Length; index++) BinaryPrimitives.WriteInt32BigEndian(output.AsSpan(index * sizeof(int)), words[index]);

        return output;
    }

    internal static byte[] Image(params (string Id, byte[] Bytes)[] extra)
    {
        using var body = new MemoryStream();
        body.Write(Encoding.ASCII.GetBytes(BeamfileMd5Host423Names.Beam));
        byte[] name = Encoding.UTF8.GetBytes(BeamfileMd5Host423Names.Subject);
        var chunks = new List<(string Id, byte[] Bytes)>
        {
            (BeamfileMd5Host423Names.Atoms, [.. Words(1), (byte)name.Length, .. name]),
            (BeamfileMd5Host423Names.Code, [.. Words(
                16,
                0,
                999,
                2,
                0
            ), 255, 254, 253]),
            (BeamfileMd5Host423Names.Imports, Words(0)),
            (BeamfileMd5Host423Names.Exports, Words(0))
        };
        chunks.AddRange(extra);
        foreach (var chunk in chunks)
        {
            body.Write(Encoding.ASCII.GetBytes(chunk.Id));
            body.Write(Words(chunk.Bytes.Length));
            body.Write(chunk.Bytes);
            for (int index = 0; index < ((-chunk.Bytes.Length) & 3); index++) body.WriteByte(0);
        }

        return [.. Encoding.ASCII.GetBytes(BeamfileMd5Host423Names.Form), .. Words((int)body.Length), .. body.ToArray()];
    }
}
