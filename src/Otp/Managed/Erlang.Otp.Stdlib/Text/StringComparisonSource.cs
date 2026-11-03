namespace Erlang;

internal static class StringComparisonSource
{
    internal const string Equal = "equal_1";
    internal const string EqualFold = "equal_nocase";
    internal const string EqualNormalized = "equal_norm";
    internal const string EqualNormalizedFold = "equal_norm_nocase";
    internal const string GraphemeLength = "str_to_gcl_and_length";
    internal const string GraphemeLoop = "gcl_and_length";
    internal const string IndexMap = "str_to_indexmap";
    internal const string MapLoop = "str_to_map";
    internal const int EqualThreeHead = 304;
    internal const int EqualFourHead = 341;
    internal const int EqualCall = 1173;
    internal const int EqualFoldCall = 1181;
    internal const int EqualNormalizedCall = 1190;
    internal const int EqualNormalizedFoldCall = 1199;
    internal const int SimilarityLeft = 1062;
    internal const int SimilarityRight = 1063;
    internal const int GraphemeStart = 2377;
    internal const int GraphemeNext = 2380;
    internal const int GraphemeError = 2384;
    internal const int IndexStart = 2388;
    internal const int IndexNext = 2393;
    internal const int IndexError = 2396;
}
