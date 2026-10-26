namespace Erlang;

internal static class EtfEncodingParameters
{
    public const int DefaultMinorVersion = 2;
    public const int MaximumMinorVersion = 2;
    public const int MinimumMinorVersion = 0;
    public const int DefaultCompressionLevel = -1;
    public const int MaximumCompressionLevel = 9;
    public const int MinimumCompressionLevel = -1;
    public const int Uncompressed = 0;
    public const int MaximumStringLength = ushort.MaxValue;
    public const int MaximumStringByte = byte.MaxValue;
    public const int MaximumSmallAtomBytes = byte.MaxValue;
    public const int LocalHashBytes = sizeof(uint);
    public const int LegacyFloatPrecision = 20;
    public const int ReferenceWordBits = 32;
}
