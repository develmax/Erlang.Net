namespace Erlang;

internal static class RecordPredicateParameters
{
    public const int SmallIntegerMagnitudeBits = 59;
    public const long MinimumSmallInteger = -(1L << SmallIntegerMagnitudeBits);
    public const long MaximumSmallInteger = (1L << SmallIntegerMagnitudeBits) - 1;
}
