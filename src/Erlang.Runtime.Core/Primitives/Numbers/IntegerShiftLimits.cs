namespace Erlang;

internal static class IntegerShiftLimits
{
    // CLR BigInteger shift counts and result bit indexing use Int32.
    public const int MaximumResultBits = int.MaxValue;
}
