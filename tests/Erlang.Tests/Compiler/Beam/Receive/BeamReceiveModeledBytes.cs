using System.Buffers.Binary;
using System.Collections;
using System.Reflection;
using System.Text;
using Erlang;

namespace Erlang.Tests;

internal static class BeamReceiveModeledBytes
{
    public static byte[] WithoutSelfSend(byte[] genuine)
    {
        BeamModule prepared = BeamModule.Prepare(genuine);
        object linked = typeof(BeamModule).GetField(BeamReceiveHostNames.CodeField, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(prepared)!;
        var instructions = (IEnumerable)linked.GetType().GetProperty(BeamReceiveHostNames.InstructionProperty)!.GetValue(linked)!;
        int sendOffset = -1;
        foreach (object instruction in instructions)
        {
            object definition = instruction.GetType().GetProperty(BeamReceiveHostNames.DefinitionProperty)!.GetValue(instruction)!;
            if ((string)definition.GetType().GetProperty(BeamReceiveHostNames.NameProperty)!.GetValue(definition)! == BeamReceiveHostNames.SendInstruction)
            {
                sendOffset = (int)instruction.GetType().GetProperty(BeamReceiveHostNames.OffsetProperty)!.GetValue(instruction)!;
                break;
            }
        }
        if (sendOffset < 0)
            throw new InvalidDataException(BeamReceiveHostNames.Failed);
        using var output = new MemoryStream();
        output.Write(genuine.AsSpan(0, BeamReceiveHostNames.FormHeaderBytes + BeamReceiveHostNames.FormTypeBytes));
        int cursor = BeamReceiveHostNames.FormHeaderBytes + BeamReceiveHostNames.FormTypeBytes;
        Span<byte> size = stackalloc byte[BeamReceiveHostNames.SignedFieldBytes];
        while (cursor < genuine.Length)
        {
            string id = Encoding.ASCII.GetString(genuine, cursor, BeamReceiveHostNames.ChunkIdentifierBytes);
            int length = BinaryPrimitives.ReadInt32BigEndian(genuine.AsSpan(cursor + BeamReceiveHostNames.ChunkSizeOffset));
            int padded = (length + BeamReceiveHostNames.ChunkPaddingMask) & ~BeamReceiveHostNames.ChunkPaddingMask;
            ReadOnlySpan<byte> payload = genuine.AsSpan(cursor + BeamReceiveHostNames.ChunkHeaderBytes, length);
            if (id == BeamReceiveHostNames.CodeChunk)
            {
                int header = BinaryPrimitives.ReadInt32BigEndian(payload) + BeamReceiveHostNames.SignedFieldBytes;
                int replacement = header + sendOffset;
                using var changed = new MemoryStream();
                changed.Write(payload[..replacement]);
                changed.Write(new byte[] { BeamReceiveHostNames.MoveOpcode, BeamReceiveHostNames.EncodedX0, BeamReceiveHostNames.EncodedX0 });
                changed.Write(payload[(replacement + 1)..]);
                byte[] bytes = changed.ToArray();
                output.Write(genuine.AsSpan(cursor, BeamReceiveHostNames.ChunkIdentifierBytes));
                BinaryPrimitives.WriteInt32BigEndian(size, bytes.Length);
                output.Write(size);
                output.Write(bytes);
                int padding = ((bytes.Length + BeamReceiveHostNames.ChunkPaddingMask) & ~BeamReceiveHostNames.ChunkPaddingMask) - bytes.Length;
                output.Write(new byte[padding]);
            }
            else
                output.Write(genuine.AsSpan(cursor, BeamReceiveHostNames.ChunkHeaderBytes + padded));
            cursor += BeamReceiveHostNames.ChunkHeaderBytes + padded;
        }
        byte[] result = output.ToArray();
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(BeamReceiveHostNames.FormSizeOffset), result.Length - BeamReceiveHostNames.FormHeaderBytes);

        return result;
    }
}
