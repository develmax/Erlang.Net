namespace Erlang;

internal static class SequenceTraceParameters
{
    public const int SerialPayloadBits = 60;
    public const ulong SerialSign = 1UL << (SerialPayloadBits - 1);
    public const ulong SerialRange = 1UL << SerialPayloadBits;
    public const ulong SerialMask = SerialRange - 1;
    public const long MinimumSerial = -(long)SerialSign;
    public const long MaximumSerial = (long)SerialSign - 1;

    public static long Signed(ulong value)
    {
        value &= SerialMask;

        return value >= SerialSign ? (long)value - (long)SerialRange : (long)value;
    }

    public static ulong Unsigned(long value) => unchecked((ulong)value) & SerialMask;
}
