using System.Numerics;

namespace Erlang.Compiler;

internal static class BitConstruction
{
    public static BitString Create(IReadOnlyList<(Term Value, Term? Size, BitSegment Segment)> segments, bool encodeFloatInOrder = false)
    {
        var expanded = new List<(Term Value, Term? Size, BitSegment Segment)>();
        var validationOnly = new HashSet<int>();
        foreach (var (value, size, segment) in segments)
        {
            if (!segment.IsStringLiteral)
            {
                expanded.Add((value, size, segment));
                continue;
            }

            var scalar = segment with { IsStringLiteral = false };
            int start = expanded.Count;
            foreach (var character in Cons.Items(value))
                expanded.Add((character, size, scalar));
            if (expanded.Count == start)
            {
                // OTP validates an empty string using scalar zero, without emitting it.
                validationOnly.Add(expanded.Count);
                expanded.Add((Term.I(0), size, scalar));
            }
        }
        segments = expanded;

        var sizes = new int[segments.Count];
        var encoded = new byte[]?[segments.Count];
        long total = 0;
        for (int i = 0; i < sizes.Length; i++)
        {
            var (value, size, segment) = segments[i];

            if (BitUnicode.IsUtf(segment.Type))
            {
                if (size is not null || segment.Unit != 1)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                encoded[i] = BitUnicode.Encode(value, segment.Type, segment.Endian);
                sizes[i] = encoded[i]!.Length * BitStorageLayout.BitsPerByte;
                total += sizes[i];
                if (total > int.MaxValue - BitStorageLayout.ByteRoundingOffset)
                    throw new ErlangException(ErlangErrorReasons.SystemLimit);
                continue;
            }

            bool integer = segment.Type == BitSegmentTypes.Integer, floating = segment.Type == BitSegmentTypes.Float, binary = segment.Type == BitSegmentTypes.Binary;
            if (integer ? value is not Integer : floating ? value is not (Integer or FloatTerm) : value is not BitString)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            BigInteger bits;
            bool whole = size is null || binary && size is Atom { Name: BitSizeAtoms.All };
            if (whole)
                bits = integer ? BitSyntaxDefaults.IntegerWidth * segment.Unit : floating ? BitSyntaxDefaults.FloatWidth * segment.Unit : ((BitString)value).BitLength;
            else if (size is Integer number && number.Value >= 0)
                bits = number.Value * segment.Unit;
            else
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (bits > int.MaxValue)
                throw new ErlangException(ErlangErrorReasons.SystemLimit);
            int count = (int)bits;
            if (floating && count is not (FloatSegmentWidths.Half or FloatSegmentWidths.Single or FloatSegmentWidths.Double))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            // eval_bits materializes each field before the next one. Compiled BEAM
            // can validate segment sizes before it attempts float encoding.
            if (floating && encodeFloatInOrder)
                encoded[i] = BitFloat.Encode(value, count, segment.Endian);
            if (binary && (count > ((BitString)value).BitLength || whole && count % segment.Unit != 0))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (validationOnly.Contains(i))
                continue;
            total += count;
            if (total > int.MaxValue - BitStorageLayout.ByteRoundingOffset)
                throw new ErlangException(ErlangErrorReasons.SystemLimit);
            sizes[i] = count;
        }

        var bytes = new byte[((int)total + BitStorageLayout.ByteRoundingOffset) / BitStorageLayout.BitsPerByte];
        int position = 0;

        void Append(int bit)
        {
            if (bit != 0)
                bytes[position / BitStorageLayout.BitsPerByte] |= (byte)(1 << (BitStorageLayout.MostSignificantBitIndex - position % BitStorageLayout.BitsPerByte));
            position++;
        }

        for (int i = 0; i < segments.Count; i++)
        {
            if (validationOnly.Contains(i))
                continue;
            var (value, _, segment) = segments[i];
            int size = sizes[i];
            if (segment.Type == BitSegmentTypes.Integer && value is Integer integer)
            {
                bool little = segment.Endian == BitByteOrders.Little || segment.Endian == BitByteOrders.Native && BitConverter.IsLittleEndian;
                if (!little)
                    for (int bit = size - 1; bit >= 0; bit--)
                        Append((int)((integer.Value >> bit) & BigInteger.One));
                else
                    for (int start = 0; start < size; start += BitStorageLayout.BitsPerByte)
                        for (int bit = Math.Min(size - start, BitStorageLayout.BitsPerByte) - 1; bit >= 0; bit--)
                            Append((int)((integer.Value >> (start + bit)) & BigInteger.One));
            }
            else
            {
                byte[] source = encoded[i] ?? (segment.Type == BitSegmentTypes.Float ? BitFloat.Encode(value, size, segment.Endian) : ((BitString)value).ToArray());
                for (int bit = 0; bit < size; bit++)
                    Append((source[bit / BitStorageLayout.BitsPerByte] >> (BitStorageLayout.MostSignificantBitIndex - bit % BitStorageLayout.BitsPerByte)) & 1);
            }
        }

        return new BitString(bytes, (int)total);
    }
}
