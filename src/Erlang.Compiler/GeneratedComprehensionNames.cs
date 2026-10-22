using System.Globalization;

namespace Erlang.Compiler;

internal static class GeneratedComprehensionNames
{
    internal const string List = "lc";
    internal const string ZipList = "zlc";
    internal const string Binary = "lbc";
    internal const string Prefix = "-";
    internal const string AritySeparator = "/";
    internal const string CoreOrdinalMarker = "$^";
    internal const string Separator = "-";

    internal static string Function(
        string owner,
        int ownerArity,
        string kind,
        int coreOrdinal,
        int arity,
        int liftedOrdinal
    )
        => Prefix + owner + AritySeparator + ownerArity.ToString(CultureInfo.InvariantCulture)
            + Separator + kind + CoreOrdinalMarker + coreOrdinal.ToString(CultureInfo.InvariantCulture)
            + AritySeparator + arity.ToString(CultureInfo.InvariantCulture)
            + Separator + liftedOrdinal.ToString(CultureInfo.InvariantCulture) + Separator;
}
