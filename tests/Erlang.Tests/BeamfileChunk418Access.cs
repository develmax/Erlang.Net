using System.Buffers.Binary;

namespace Erlang.Tests;

internal static class BeamfileChunk418Access
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(BeamfileChunk418Names.Failure);
    }

    internal static ErlangFunction Capture(ProcessRuntime runtime)
    {
        object?[] arguments = [runtime, BeamfileChunk418Names.Module, BeamfileChunk418Names.Function, BeamfileChunk418Names.Arity, null];
        bool found = (bool)typeof(ProcessRuntime).Assembly.GetType(BeamfileChunk418Names.RuntimeType, true)!
            .GetMethod(BeamfileChunk418Names.Capture, BeamfileChunk418Names.Static)!.Invoke(null, arguments)!;
        Require(found && arguments[4] is ErlangFunction);

        return (ErlangFunction)arguments[4]!;
    }

    internal static Term Identifier => Cons.From(BeamfileChunk418Names.Identifier.Select(letter => Term.I(letter)));

    internal static byte[] Chunk(uint id, byte[] data)
    {
        byte[] result = new byte[BeamfileChunk418Names.HeaderBytes + ((data.Length + 3) & ~3)];
        BinaryPrimitives.WriteUInt32BigEndian(result, id);
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(BeamfileChunk418Names.WordBytes), data.Length);
        data.CopyTo(result, BeamfileChunk418Names.HeaderBytes);

        return result;
    }

    internal static byte[] Form(params byte[][] chunks)
    {
        int body = BeamfileChunk418Names.WordBytes + chunks.Sum(chunk => chunk.Length);
        byte[] result = new byte[BeamfileChunk418Names.HeaderBytes + body];
        BinaryPrimitives.WriteUInt32BigEndian(result, BeamfileChunk418Names.FormId);
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(BeamfileChunk418Names.WordBytes), body);
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(BeamfileChunk418Names.HeaderBytes), BeamfileChunk418Names.BeamId);
        int offset = BeamfileChunk418Names.HeaderBytes + BeamfileChunk418Names.WordBytes;
        foreach (byte[] chunk in chunks)
        {
            chunk.CopyTo(result, offset);
            offset += chunk.Length;
        }

        return result;
    }

    internal static BitString Unaligned(byte[] value)
    {
        byte[] container = new byte[value.Length + 1];
        container[0] = 128;
        for (int index = 0; index < value.Length; index++)
        {
            container[index] |= (byte)(value[index] >> 1);
            container[index + 1] |= (byte)(value[index] << 7);
        }

        return new BitString(container).SliceBits(1, value.Length * BeamfileChunk418Names.ByteBits);
    }
}
