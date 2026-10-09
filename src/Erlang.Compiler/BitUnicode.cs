using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace Erlang.Compiler;

internal static class BitUnicode
{
    internal static bool IsUtf(string type) => type is "utf8" or "utf16" or "utf32";
    private static bool Little(string endian) => endian == "little" || endian == "native" && BitConverter.IsLittleEndian;

    internal static byte[] Encode(Term value, string type, string endian)
    {
        if (value is not Integer integer || integer.Value < 0 || integer.Value > 0x10ffff || !Rune.TryCreate((int)integer.Value, out var scalar))
            throw new ErlangException("badarg");
        if (type == "utf8")
        {
            var bytes = new byte[scalar.Utf8SequenceLength]; scalar.EncodeToUtf8(bytes); return bytes;
        }
        if (type == "utf16")
        {
            Span<char> chars = stackalloc char[2]; int length = scalar.EncodeToUtf16(chars);
            var bytes = new byte[length * 2];
            for (int i = 0; i < length; i++)
                if (Little(endian)) BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(i * 2), chars[i]);
                else BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(i * 2), chars[i]);
            return bytes;
        }
        var result = new byte[4];
        if (Little(endian)) BinaryPrimitives.WriteUInt32LittleEndian(result, (uint)scalar.Value);
        else BinaryPrimitives.WriteUInt32BigEndian(result, (uint)scalar.Value);
        return result;
    }

    internal static Integer? Decode(ReadOnlySpan<byte> bytes, string type, string endian, out int consumed)
    {
        consumed = 0;
        if (type == "utf8")
            return Rune.DecodeFromUtf8(bytes, out var scalar, out consumed) == OperationStatus.Done ? new Integer(scalar.Value) : null;
        if (type == "utf16")
        {
            Span<char> chars = stackalloc char[2]; int length = Math.Min(bytes.Length / 2, 2);
            for (int i = 0; i < length; i++) chars[i] = (char)(Little(endian) ? BinaryPrimitives.ReadUInt16LittleEndian(bytes[(i * 2)..]) : BinaryPrimitives.ReadUInt16BigEndian(bytes[(i * 2)..]));
            if (Rune.DecodeFromUtf16(chars[..length], out var scalar, out int count) != OperationStatus.Done) return null;
            consumed = count * 2; return new Integer(scalar.Value);
        }
        if (bytes.Length < 4) return null;
        uint value = Little(endian) ? BinaryPrimitives.ReadUInt32LittleEndian(bytes) : BinaryPrimitives.ReadUInt32BigEndian(bytes);
        if (!Rune.TryCreate(value, out _)) return null;
        consumed = 4; return new Integer(value);
    }
}
