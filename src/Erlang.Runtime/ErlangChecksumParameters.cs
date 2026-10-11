namespace Erlang;

internal static class ErlangChecksumParameters
{
    public const uint AdlerModulus = 65521;
    public const int AdlerHalfWidth = 16;
    public const uint AdlerHalfMask = 0xffff;
    public const uint CrcReflectedPolynomial = 0xedb88320;
    public const int CrcWidth = 32;
}
