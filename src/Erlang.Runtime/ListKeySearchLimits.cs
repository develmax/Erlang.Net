using System.Numerics;

namespace Erlang;

internal static class ListKeySearchLimits
{
    public const int ReferenceSmallMagnitudeBits = 59;
    public static readonly BigInteger MinimumSmallInteger = -(BigInteger.One << ReferenceSmallMagnitudeBits);
    public static readonly BigInteger MaximumSmallInteger = (BigInteger.One << ReferenceSmallMagnitudeBits) - 1;
}
