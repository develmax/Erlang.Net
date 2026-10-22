namespace Erlang;

internal static class ErlangFloatParameters
{
    public const int OutputBufferBytes = 256;
    public const int MaximumFixedPrecision = 253;
    public const int MaximumScientificPrecision = 249;
    public const int MaximumFastPrecision = 18;
    public const double ExactIntegerBoundary = 9007199254740992.0;
    public const long MinimumSmallInteger = -(1L << 59);
    public const long MaximumSmallInteger = (1L << 59) - 1;
    public const string DecimalPoint = ".";
    public const string ExponentMarker = "e";
    public const string NegativeSign = "-";
    public const string PositiveSign = "+";
    public const string Zero = "0";
    public const string FloatZero = "0.0";
    public const string FixedFormat = "F";
    public const string IntegerFormat = "F0";
    public const string RoundTripFormat = "R";
}
