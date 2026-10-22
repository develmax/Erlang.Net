using System.Numerics;

namespace Erlang;

internal static class BinaryUnsigned
{
    private static bool BigEndian(Term value)
    {
        if (value is not Atom { Name: BinaryPrimitiveParameters.BigEndian or BinaryPrimitiveParameters.LittleEndian } order)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return order.Name == BinaryPrimitiveParameters.BigEndian;
    }

    internal static async ValueTask<Term> Decode(ProcessContext context, Term value, Term endian)
    {
        BitString bits = BinaryPrimitiveOperations.Binary(value);
        bool big = BigEndian(endian);
        for (int index = 0; index < bits.ByteLength; index += BinaryPrimitiveParameters.CopyChunkBytes)
            await context.ReduceAsync();
        BigInteger result = new(bits.ByteSpan, isUnsigned: true, isBigEndian: big);
        await context.ReduceAsync();

        return new Integer(result);
    }

    internal static async ValueTask<Term> Encode(ProcessContext context, Term value, Term endian)
    {
        if (value is not Integer { Value.Sign: >= 0 } number)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        bool big = BigEndian(endian);
        int byteCount = number.Value.GetByteCount(isUnsigned: true);
        if (byteCount > BinaryPrimitiveParameters.MaximumBinaryBytes)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
        for (int index = 0; index < byteCount; index += BinaryPrimitiveParameters.CopyChunkBytes)
            await context.ReduceAsync();
        var bytes = number.Value.ToByteArray(isUnsigned: true, isBigEndian: big);
        await context.ReduceAsync();

        return BitString.FromOwnedBytes(bytes);
    }
}
