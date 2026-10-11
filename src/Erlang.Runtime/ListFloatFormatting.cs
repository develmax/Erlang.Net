namespace Erlang;

internal static class ListFloatFormatting
{
    public static string Format(double value) => ErlangFloatScientific.Format(value, ListFloatFormat.FractionDigits);
}
