// Derived from Erlang/OTP unicode.erl (Apache-2.0); see licenses/.
using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace Erlang;

internal readonly record struct UnicodeEncoding(int Width, bool Little = false)
{
    internal static UnicodeEncoding Parse(Term value) => value switch
    {
        Atom { Name: UnicodeNames.Latin1 } => new(1),
        Atom { Name: UnicodeNames.Utf8 or UnicodeNames.Unicode } => new(0),
        Atom { Name: UnicodeNames.Utf16 } => new(2),
        Atom { Name: UnicodeNames.Utf32 } => new(4),
        TupleTerm { Items.Count: 2 } pair when pair.Items[0] is Atom { Name: UnicodeNames.Utf16 or UnicodeNames.Utf32 } name
            && pair.Items[1] is Atom { Name: UnicodeNames.Big or UnicodeNames.Little } endian
            => new(name.Name == UnicodeNames.Utf16 ? 2 : 4, endian.Name == UnicodeNames.Little),
        _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
    };

    internal bool Decode(ReadOnlySpan<byte> source, out int scalar, out int used)
    {
        scalar = -1;
        used = Width;
        if (Width == 1)
        {
            scalar = source[0];
        }
        else if (Width == 0)
        {
            if (Rune.DecodeFromUtf8(source, out Rune rune, out used) != OperationStatus.Done)
            {
                return false;
            }
            scalar = rune.Value;
        }
        else if (source.Length >= Width)
        {
            if (Width == 4)
            {
                uint value = Little ? BinaryPrimitives.ReadUInt32LittleEndian(source) : BinaryPrimitives.ReadUInt32BigEndian(source);
                scalar = value <= UnicodeNames.Maximum ? (int)value : -1;
            }
            else
            {
                int first = Word(source);
                if (first is >= 0xd800 and <= 0xdbff)
                {
                    used = 4;
                    if (source.Length < used)
                    {
                        return false;
                    }
                    int second = Word(source[2..]);
                    scalar = second is >= 0xdc00 and <= 0xdfff ? 0x10000 + ((first - 0xd800) << 10) + second - 0xdc00 : -1;
                }
                else
                {
                    scalar = first;
                }
            }
        }

        return scalar >= 0 && scalar is not (>= UnicodeNames.SurrogateStart and <= UnicodeNames.SurrogateEnd);
    }

    // OTP cbv deliberately accepts some prefixes which a strict .NET decoder rejects.
    internal int Missing(ReadOnlySpan<byte> data)
    {
        int count = data.Length;
        if (Width == 0)
        {
            int total = (data[0] & 0xe0) == 0xc0 ? 2 : (data[0] & 0xf0) == 0xe0 ? 3 : (data[0] & 0xf8) == 0xf0 ? 4 : 0;
            if (count >= total)
            {
                return 0;
            }
            for (int index = 1; index < count; index++)
            {
                if ((data[index] & 0xc0) != 0x80)
                {
                    return 0;
                }
            }

            return total - count;
        }
        if (Width == 2)
        {
            if (count == 1)
            {
                return Little || data[0] <= 215 || data[0] >= 224 ? 1 : data[0] >> 2 == 54 ? 3 : 0;
            }
            if (count is 2 or 3 && data[Little ? 1 : 0] >> 2 == 54)
            {
                return count == 2 ? 2 : Little || data[2] >> 2 == 55 ? 1 : 0;
            }
        }
        if (Width == 4 && count < 4)
        {
            if (Little)
            {
                if (count < 3)
                {
                    return 4 - count;
                }
                if (data[0] is 254 or 255 && data[1] == 255 && data[2] == 0)
                {
                    return 0;
                }

                return data[2] <= 16 && (data[2] > 0 || data[1] <= 215 || data[1] >= 224) ? 1 : 0;
            }
            if (data[0] == 0 && (count == 1 || data[1] <= 16 && (count == 2 || data[1] > 0 || data[2] <= 215 || data[2] >= 224)))
            {
                return 4 - count;
            }
        }

        return 0;
    }

    internal void Append(List<byte> bytes, int scalar)
    {
        Span<byte> buffer = stackalloc byte[4];
        int length;
        if (Width == 1)
        {
            bytes.Add((byte)scalar);

            return;
        }
        var rune = new Rune(scalar);
        if (Width == 0)
        {
            length = rune.EncodeToUtf8(buffer);
        }
        else if (Width == 4)
        {
            if (Little)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(buffer, (uint)scalar);
            }
            else
            {
                BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)scalar);
            }
            length = 4;
        }
        else
        {
            Span<char> chars = stackalloc char[2];
            length = rune.EncodeToUtf16(chars) * 2;
            for (int index = 0; index < length / 2; index++)
            {
                if (Little)
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(buffer[(index * 2)..], chars[index]);
                }
                else
                {
                    BinaryPrimitives.WriteUInt16BigEndian(buffer[(index * 2)..], chars[index]);
                }
            }
        }
        for (int index = 0; index < length; index++)
        {
            bytes.Add(buffer[index]);
        }
    }

    private int Word(ReadOnlySpan<byte> source)
        => Little ? BinaryPrimitives.ReadUInt16LittleEndian(source) : BinaryPrimitives.ReadUInt16BigEndian(source);
}
