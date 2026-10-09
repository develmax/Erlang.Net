using System.Numerics;

namespace Erlang.Compiler;

internal static class BitConstruction
{
    public static BitString Create(IReadOnlyList<(Term Value, Term? Size, BitSegment Segment)> segments)
    {
        var sizes = new int[segments.Count]; long total = 0;
        for (int i = 0; i < sizes.Length; i++)
        {
            var (value, size, segment) = segments[i];
            bool integer = segment.Type == "integer";
            if (integer ? value is not Integer : value is not BitString) throw new ErlangException("badarg");
            BigInteger bits;
            bool whole = size is null || !integer && size is Atom { Name: "all" };
            if (whole) bits = integer ? 8 * segment.Unit : ((BitString)value).BitLength;
            else if (size is Integer number && number.Value >= 0) bits = number.Value * segment.Unit;
            else throw new ErlangException("badarg");
            if (bits > int.MaxValue) throw new ErlangException("system_limit");
            int count = (int)bits;
            if (!integer && (count > ((BitString)value).BitLength || whole && count % segment.Unit != 0)) throw new ErlangException("badarg");
            total += count; if (total > int.MaxValue - 7) throw new ErlangException("system_limit");
            sizes[i] = count;
        }
        var bytes = new byte[((int)total + 7) / 8]; int position = 0;
        void Append(int bit) { if (bit != 0) bytes[position / 8] |= (byte)(1 << (7 - position % 8)); position++; }
        for (int i = 0; i < segments.Count; i++)
        {
            var (value, _, segment) = segments[i]; int size = sizes[i];
            if (value is Integer integer)
            {
                bool little = segment.Endian == "little" || segment.Endian == "native" && BitConverter.IsLittleEndian;
                if (!little)
                    for (int bit = size - 1; bit >= 0; bit--) Append((int)((integer.Value >> bit) & BigInteger.One));
                else
                    for (int start = 0; start < size; start += 8)
                        for (int bit = Math.Min(size - start, 8) - 1; bit >= 0; bit--) Append((int)((integer.Value >> (start + bit)) & BigInteger.One));
            }
            else
            {
                byte[] source = ((BitString)value).ToArray();
                for (int bit = 0; bit < size; bit++) Append((source[bit / 8] >> (7 - bit % 8)) & 1);
            }
        }
        return new BitString(bytes, (int)total);
    }
}
