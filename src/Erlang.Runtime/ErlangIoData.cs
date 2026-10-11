using System.Numerics;

namespace Erlang;

internal static class ErlangIoData
{
    public static BitString Bits(Term value)
    {
        if (value is not BitString bits)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return bits;
    }

    public static byte[] Binary(Term value)
    {
        BitString bits = Bits(value);
        if (!bits.IsBinary)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return bits.ToArray();
    }

    public static IEnumerable<BitString> Segments(Term value, bool allowBits, bool requireList)
    {
        if (value is not Nil and not Cons && (requireList || value is not BitString))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var pending = new Stack<(Term Value, bool Tail)>();
        var integers = new List<byte>(ErlangDataLimits.IntegerByteBatchCapacity);
        pending.Push((value, true));
        while (pending.TryPop(out var item))
        {
            switch (item.Value)
            {
                case Nil:
                    break;
                case Cons cell:
                    pending.Push((cell.Tail, true));
                    pending.Push((cell.Head, false));
                    break;
                case Integer number when !item.Tail && number.Value >= 0 && number.Value <= ErlangDataLimits.MaximumByte:
                    integers.Add((byte)number.Value);
                    if (integers.Count == ErlangDataLimits.IntegerByteBatchCapacity)
                    {
                        yield return new BitString(integers.ToArray());
                        integers.Clear();
                    }
                    break;
                case BitString bits when allowBits || bits.IsBinary:
                    if (integers.Count != 0)
                    {
                        yield return new BitString(integers.ToArray());
                        integers.Clear();
                    }
                    yield return bits;
                    break;
                default:
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }
        if (integers.Count != 0)
            yield return new BitString(integers.ToArray());
    }

    public static Term Size(Term value)
    {
        BigInteger size = 0;
        foreach (BitString segment in Segments(value, false, false))
            size += segment.BitLength / ErlangDataLimits.BitsPerByte;

        return new Integer(size);
    }

    public static Term Iovec(Term value)
    {
        var result = new List<Term>();
        using var buffer = new MemoryStream();
        foreach (BitString segment in Segments(value, false, false))
        {
            if (segment.BitLength / ErlangDataLimits.BitsPerByte <= ErlangDataLimits.IovecCopyThresholdBytes)
            {
                buffer.Write(segment.ToArray());
                continue;
            }
            Flush();
            result.Add(segment);
        }
        Flush();

        return Cons.From(result);

        void Flush()
        {
            if (buffer.Length == 0)
                return;
            result.Add(new BitString(buffer.ToArray()));
            buffer.SetLength(0);
        }
    }

    public static BitString Flatten(Term value, bool allowBits, bool requireList)
    {
        BitString[] segments = Segments(value, allowBits, requireList).ToArray();
        long length = segments.Sum(segment => (long)segment.BitLength);
        if (length > int.MaxValue)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
        byte[] result = new byte[(length + ErlangDataLimits.BitsPerByte - 1) / ErlangDataLimits.BitsPerByte];
        int position = 0;
        foreach (BitString segment in segments)
        {
            byte[] bytes = segment.ToArray();
            if (position % ErlangDataLimits.BitsPerByte == 0 && segment.IsBinary)
                bytes.CopyTo(result, position / ErlangDataLimits.BitsPerByte);
            else
                for (int index = 0; index < segment.BitLength; index++)
                    if ((bytes[index / ErlangDataLimits.BitsPerByte] & (1 << (ErlangDataLimits.BitsPerByte - 1 - index % ErlangDataLimits.BitsPerByte))) != 0)
                        result[(position + index) / ErlangDataLimits.BitsPerByte] |= (byte)(1 << (ErlangDataLimits.BitsPerByte - 1 - (position + index) % ErlangDataLimits.BitsPerByte));
            position += segment.BitLength;
        }

        return new BitString(result, (int)length);
    }

    public static Term ToList(Term value, bool allowBits)
    {
        BitString bits = Bits(value);
        if (!allowBits && !bits.IsBinary)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        byte[] bytes = bits.ToArray();
        var items = bytes.Take(bits.BitLength / ErlangDataLimits.BitsPerByte).Select(number => (Term)Term.I(number)).ToList();
        if (!bits.IsBinary)
            items.Add(new BitString(new[] { bytes[^1] }, bits.BitLength % ErlangDataLimits.BitsPerByte));

        return Cons.From(items);
    }

    public static Term SliceList(IReadOnlyList<Term> arguments)
    {
        byte[] bytes = Binary(arguments[0]);
        if (arguments[1] is not Integer first || arguments[2] is not Integer last || first.Value < 1 || last.Value < first.Value || last.Value > bytes.Length)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return Cons.From(bytes.Skip((int)first.Value - 1).Take((int)(last.Value - first.Value + 1)).Select(number => (Term)Term.I(number)));
    }

    public static Term Part(Term binary, Term position, Term length)
    {
        byte[] bytes = Binary(binary);
        if (position is not Integer start || length is not Integer count || start.Value < 0)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        BigInteger offset = count.Value < 0 ? start.Value + count.Value : start.Value;
        BigInteger size = BigInteger.Abs(count.Value);
        if (offset < 0 || offset + size > bytes.Length)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return new BitString(bytes.AsSpan((int)offset, (int)size));
    }

    public static Term Split(Term value, Term offset)
    {
        BitString bits = Bits(value);
        if (offset is not Integer index || index.Value < 0 || index.Value > bits.BitLength / ErlangDataLimits.BitsPerByte)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        int position = (int)index.Value;
        byte[] bytes = bits.ToArray();

        return Term.Tuple(
            new BitString(bytes.AsSpan(0, position)),
            new BitString(bytes.AsSpan(position), bits.BitLength - position * ErlangDataLimits.BitsPerByte)
        );
    }
}
