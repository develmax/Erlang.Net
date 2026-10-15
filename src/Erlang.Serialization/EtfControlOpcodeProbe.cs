using System.Buffers.Binary;

namespace Erlang;

internal static class EtfControlOpcodeProbe
{
    internal static int? Read(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < EtfControlIntegerLayout.SmallTupleBodyOffset || bytes[EtfHeaderLayout.VersionOffset] != EtfHeaderLayout.Version)
            return null;
        int offset;
        if (bytes[EtfHeaderLayout.TagOffset] == EtfTags.SmallTuple)
        {
            if (bytes[EtfControlIntegerLayout.SmallTupleArityOffset] == 0)
                return null;
            offset = EtfControlIntegerLayout.SmallTupleBodyOffset;
        }
        else if (bytes[EtfHeaderLayout.TagOffset] == EtfTags.LargeTuple)
        {
            if (bytes.Length < EtfControlIntegerLayout.LargeTupleBodyOffset ||
                BinaryPrimitives.ReadUInt32BigEndian(bytes[EtfControlIntegerLayout.LargeTupleArityOffset..]) == 0)
                return null;
            offset = EtfControlIntegerLayout.LargeTupleBodyOffset;
        }
        else
            return null;
        if (offset >= bytes.Length)
            return null;
        ReadOnlySpan<byte> number = bytes[offset..];
        if (number[0] == EtfTags.SmallInteger)
            return number.Length >= EtfHeaderLayout.MinimumPacketBytes ? number[EtfControlIntegerLayout.ScalarBodyOffset] : null;
        if (number[0] == EtfTags.Integer)
            return number.Length >= EtfControlIntegerLayout.ScalarBodyOffset + sizeof(int)
                ? BinaryPrimitives.ReadInt32BigEndian(number[EtfControlIntegerLayout.ScalarBodyOffset..]) : null;
        int body, sign;
        uint size;
        if (number[0] == EtfTags.SmallBig)
        {
            if (number.Length < EtfControlIntegerLayout.SmallBigBodyOffset)
                return null;
            size = number[EtfControlIntegerLayout.ScalarBodyOffset];
            sign = number[EtfControlIntegerLayout.SmallBigSignOffset];
            body = EtfControlIntegerLayout.SmallBigBodyOffset;
        }
        else if (number[0] == EtfTags.LargeBig)
        {
            if (number.Length < EtfControlIntegerLayout.LargeBigBodyOffset)
                return null;
            size = BinaryPrimitives.ReadUInt32BigEndian(number[EtfControlIntegerLayout.ScalarBodyOffset..]);
            sign = number[EtfControlIntegerLayout.LargeBigSignOffset];
            body = EtfControlIntegerLayout.LargeBigBodyOffset;
        }
        else
            return null;
        if (sign > EtfControlIntegerLayout.NegativeSign || size > number.Length - body)
            return null;
        ReadOnlySpan<byte> magnitude = number.Slice(body, (int)size);
        int high = magnitude.Length - 1;
        while (high >= 0 && magnitude[high] == 0)
            high--;
        if (high >= EtfControlIntegerLayout.MagnitudeBytes)
            return null;
        uint value = 0;
        for (int index = 0; index <= high; index++)
            value |= (uint)magnitude[index] << (index * EtfControlIntegerLayout.BitsPerByte);
        if (value > int.MaxValue)
            return null;

        return sign == EtfControlIntegerLayout.NegativeSign ? -(int)value : (int)value;
    }
}
