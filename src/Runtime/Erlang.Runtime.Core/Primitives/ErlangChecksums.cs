using System.Security.Cryptography;

namespace Erlang;

internal static class ErlangChecksums
{
    private static readonly uint[] crcTable = BuildTable();

    public static uint Word(Term value)
    {
        if (value is not Integer number || number.Value < uint.MinValue || number.Value > uint.MaxValue)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return (uint)number.Value;
    }

    public static Term Compute(string operation, uint initial, Term data)
    {
        uint value = initial;
        foreach (BitString segment in ErlangIoData.Segments(data, false, false))
        {
            byte[] bytes = segment.ToArray();
            if (operation == ErlangChecksumNames.Crc)
            {
                uint state = ~value;
                foreach (byte number in bytes)
                    state = crcTable[(state ^ number) & ErlangDataLimits.MaximumByte] ^ (state >> ErlangDataLimits.BitsPerByte);
                value = ~state;
            }
            else
            {
                ulong first = value & ErlangChecksumParameters.AdlerHalfMask;
                ulong second = value >> ErlangChecksumParameters.AdlerHalfWidth;
                foreach (byte number in bytes)
                {
                    first = (first + number) % ErlangChecksumParameters.AdlerModulus;
                    second = (second + first) % ErlangChecksumParameters.AdlerModulus;
                }
                value = (uint)(first % ErlangChecksumParameters.AdlerModulus | ((second % ErlangChecksumParameters.AdlerModulus) << ErlangChecksumParameters.AdlerHalfWidth));
            }
        }

        return Term.I(value);
    }

    public static Term Md5(Term data)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        foreach (BitString segment in ErlangIoData.Segments(data, false, false))
            hash.AppendData(segment.ToArray());

        return new BitString(hash.GetHashAndReset());
    }

    public static Term Combine(string operation, IReadOnlyList<Term> arguments)
    {
        uint first = Word(arguments[0]);
        uint second = Word(arguments[1]);
        uint length = Word(arguments[2]);
        if (operation == ErlangChecksumNames.AdlerCombine)
        {
            if (length == 0)
                return Term.I(first);
            ulong remainder = length % ErlangChecksumParameters.AdlerModulus;
            ulong low = (first & ErlangChecksumParameters.AdlerHalfMask) + (second & ErlangChecksumParameters.AdlerHalfMask) + ErlangChecksumParameters.AdlerModulus - 1;
            ulong high = remainder * (first & ErlangChecksumParameters.AdlerHalfMask) % ErlangChecksumParameters.AdlerModulus + (first >> ErlangChecksumParameters.AdlerHalfWidth) + (second >> ErlangChecksumParameters.AdlerHalfWidth) + ErlangChecksumParameters.AdlerModulus - remainder;

            return Term.I((uint)(low % ErlangChecksumParameters.AdlerModulus | ((high % ErlangChecksumParameters.AdlerModulus) << ErlangChecksumParameters.AdlerHalfWidth)));
        }
        uint[] operatorMatrix = new uint[ErlangChecksumParameters.CrcWidth];
        for (int index = 0; index < operatorMatrix.Length; index++)
        {
            uint vector = 1u << index;
            for (int bit = 0; bit < ErlangDataLimits.BitsPerByte; bit++)
                vector = (vector >> 1) ^ ((vector & 1) == 0 ? 0 : ErlangChecksumParameters.CrcReflectedPolynomial);
            operatorMatrix[index] = vector;
        }
        while (length != 0)
        {
            if ((length & 1) != 0)
                first = Multiply(operatorMatrix, first);
            length >>= 1;
            operatorMatrix = operatorMatrix.Select(vector => Multiply(operatorMatrix, vector)).ToArray();
        }

        return Term.I(first ^ second);
    }

    private static uint Multiply(uint[] matrix, uint vector)
    {
        uint result = 0;
        int index = 0;
        while (vector != 0)
        {
            if ((vector & 1) != 0)
                result ^= matrix[index];
            vector >>= 1;
            index++;
        }

        return result;
    }

    private static uint[] BuildTable()
    {
        var table = new uint[ErlangDataLimits.MaximumByte + 1];
        for (uint index = 0; index < table.Length; index++)
        {
            uint value = index;
            for (int bit = 0; bit < ErlangDataLimits.BitsPerByte; bit++)
                value = (value >> 1) ^ ((value & 1) == 0 ? 0 : ErlangChecksumParameters.CrcReflectedPolynomial);
            table[index] = value;
        }

        return table;
    }
}
