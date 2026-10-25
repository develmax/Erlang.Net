namespace Erlang;

internal static class ErlangDataLimits
{
    public const int BitsPerByte = 8;
    public const int MaximumByte = 255;
    public const int MinimumIntegerBase = 2;
    public const int MaximumIntegerBase = 36;
    public const int DecimalIntegerBase = 10;
    public const string IntegerDigits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public const int IovecCopyThresholdBytes = 512;
    public const int IntegerByteBatchCapacity = 512;
    public const int IntegerParsingChunkDigits = 12;
}
