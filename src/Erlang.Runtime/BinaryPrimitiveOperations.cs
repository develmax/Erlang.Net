using System.Numerics;

namespace Erlang;

internal static class BinaryPrimitiveOperations
{
    internal static BitString Binary(Term value)
    {
        if (value is not BitString { IsBinary: true } bits)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return bits;
    }

    internal static Term At(Term value, Term index)
    {
        BitString bits = Binary(value);
        if (index is not Integer number || number.Value < 0 || number.Value >= bits.ByteLength)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return Term.I(bits.ByteSpan[(int)number.Value]);
    }

    internal static Term Edge(Term value, bool last)
    {
        BitString bits = Binary(value);
        if (bits.ByteLength == 0)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return Term.I(bits.ByteSpan[last ? bits.ByteLength - 1 : 0]);
    }

    internal static Term Part(Term binary, Term range)
    {
        if (range is not TupleTerm { Items.Count: 2 } pair)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return ErlangIoData.Part(binary, pair.Items[0], pair.Items[1]);
    }

    internal static async ValueTask<Term> Copy(ProcessContext context, Term value, Term count)
    {
        BitString bits = Binary(value);
        if (count is not Integer repeats || repeats.Value < 0 || repeats.Value > ulong.MaxValue)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        BigInteger size = repeats.Value * bits.ByteLength;
        if (size > BinaryPrimitiveParameters.MaximumBinaryBytes)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
        byte[] result;
        try
        {
            result = new byte[(int)size];
        }
        catch (OutOfMemoryException)
        {
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
        }
        for (int offset = 0; offset < result.Length;)
        {
            int chunk = Math.Min(BinaryPrimitiveParameters.CopyChunkBytes, result.Length - offset);
            await context.ChargeAsync((chunk + BinaryPrimitiveParameters.CopyBytesPerReduction - 1) / BinaryPrimitiveParameters.CopyBytesPerReduction);
            for (int index = 0; index < chunk; index++)
                result[offset + index] = bits.ByteSpan[(offset + index) % bits.ByteLength];
            offset += chunk;
        }

        return BitString.FromOwnedBytes(result);
    }

    internal static async ValueTask<Term> Common(ProcessContext context, Term value, bool suffix)
    {
        var binaries = new List<BitString>();
        Term remaining = value;
        int shortest = int.MaxValue;
        while (remaining is Cons cell)
        {
            await context.ReduceAsync();
            BitString binary = Binary(cell.Head);
            binaries.Add(binary);
            shortest = Math.Min(shortest, binary.ByteLength);
            remaining = cell.Tail;
        }
        if (binaries.Count >= BinaryPrimitiveParameters.MaximumCommonBinaries)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
        if (remaining is not Nil || binaries.Count == 0)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (binaries.Count == 1)
            return Term.I(shortest);
        for (int index = 0; index < shortest; index++)
        {
            await context.ReduceAsync();
            byte first = binaries[0].ByteSpan[suffix ? binaries[0].ByteLength - index - 1 : index];
            for (int other = 1; other < binaries.Count; other++)
            {
                await context.ReduceAsync();
                BitString candidate = binaries[other];
                if (candidate.ByteSpan[suffix ? candidate.ByteLength - index - 1 : index] != first)
                    return Term.I(index);
            }
        }

        return Term.I(shortest);
    }
}
