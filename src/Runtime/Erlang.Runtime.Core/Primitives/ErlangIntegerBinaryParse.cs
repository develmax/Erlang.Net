namespace Erlang;

internal static class ErlangIntegerBinaryParse
{
    internal static Integer Parse(Term original, int radix, out bool big)
    {
        big = false;
        byte[] bytes = ErlangIoData.Binary(original);
        int offset = 0;
        if (bytes.Length != 0 && bytes[0] is (byte)'+' or (byte)'-')
            offset++;
        int firstDigit = offset;
        while (offset < bytes.Length && bytes[offset] == '0')
            offset++;
        if (offset == bytes.Length && offset != firstDigit)
            return Term.I(0);
        big = bytes.Length - offset > ErlangIntegerParseParameters.DigitsPerSmall[radix - ErlangDataLimits.MinimumIntegerBase];

        return ErlangIntegerText.Parse(bytes.Select(value => (int)value), radix);
    }
}
