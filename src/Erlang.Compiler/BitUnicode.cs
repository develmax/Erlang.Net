using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace Erlang.Compiler;

internal static class BitUnicode
{
    internal static bool IsUtf(string type) => type is BitSegmentTypes.Utf8 or BitSegmentTypes.Utf16 or BitSegmentTypes.Utf32;

    private static bool Little(string endian) => endian == BitByteOrders.Little || endian == BitByteOrders.Native && BitConverter.IsLittleEndian;

    internal static byte[] Encode(Term value, string type, string endian)
    {
        if (value is not Integer integer || integer.Value < 0 || integer.Value > UtfSegmentLimits.MaximumScalar || !Rune.TryCreate((int)integer.Value, out var scalar))
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        if (type == BitSegmentTypes.Utf8)
        {
            var bytes = new byte[scalar.Utf8SequenceLength];
            scalar.EncodeToUtf8(bytes);

            return bytes;
        }

        if (type == BitSegmentTypes.Utf16)
        {
            Span<char> chars = stackalloc char[UtfSegmentLimits.MaximumUtf16Characters];
            int length = scalar.EncodeToUtf16(chars);

            var bytes = new byte[length * UtfSegmentLimits.Utf16BytesPerCharacter];

            for (int i = 0; i < length; i++)
                if (Little(endian))
                    BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(i * UtfSegmentLimits.Utf16BytesPerCharacter), chars[i]);
                else
                    BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(i * UtfSegmentLimits.Utf16BytesPerCharacter), chars[i]);

            return bytes;
        }

        var result = new byte[UtfSegmentLimits.Utf32BytesPerScalar];
        if (Little(endian))
            BinaryPrimitives.WriteUInt32LittleEndian(result, (uint)scalar.Value);
        else
            BinaryPrimitives.WriteUInt32BigEndian(result, (uint)scalar.Value);

        return result;
    }

    internal static Integer? Decode(
        ReadOnlySpan<byte> bytes,
        string type,
        string endian,
        out int consumed
    )
    {
        consumed = 0;

        if (type == BitSegmentTypes.Utf8)
            return Rune.DecodeFromUtf8(bytes, out var scalar, out consumed) == OperationStatus.Done ? new Integer(scalar.Value) : null;

        if (type == BitSegmentTypes.Utf16)
        {
            Span<char> chars = stackalloc char[UtfSegmentLimits.MaximumUtf16Characters];
            int length = Math.Min(bytes.Length / UtfSegmentLimits.Utf16BytesPerCharacter, UtfSegmentLimits.MaximumUtf16Characters);

            for (int i = 0; i < length; i++)
                chars[i] = (char)(Little(endian) ? BinaryPrimitives.ReadUInt16LittleEndian(bytes[(i * UtfSegmentLimits.Utf16BytesPerCharacter)..]) : BinaryPrimitives.ReadUInt16BigEndian(bytes[(i * UtfSegmentLimits.Utf16BytesPerCharacter)..]));

            if (Rune.DecodeFromUtf16(chars[..length], out var scalar, out int count) != OperationStatus.Done)
                return null;

            consumed = count * UtfSegmentLimits.Utf16BytesPerCharacter;

            return new Integer(scalar.Value);
        }

        if (bytes.Length < UtfSegmentLimits.Utf32BytesPerScalar)
            return null;

        uint value = Little(endian) ? BinaryPrimitives.ReadUInt32LittleEndian(bytes) : BinaryPrimitives.ReadUInt32BigEndian(bytes);
        if (!Rune.TryCreate(value, out _))
            return null;
        consumed = UtfSegmentLimits.Utf32BytesPerScalar;

        return new Integer(value);
    }
}
