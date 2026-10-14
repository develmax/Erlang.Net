namespace Erlang.Tests;

internal static class ProcessBumpCapHostNames
{
    internal const string Test = "runtime/process-bump-current-logical-slice-boundaries";
    internal const string Failure = "Process bump charged beyond remaining logical slice";
    internal const int LogicalSlice = 2000;
    internal const int NativeRequestCap = 4000;
    internal const long MaximumNativeSmall = (1L << 59) - 1;
    internal const long NativeTooLarge = 1L << 60;
    internal const int Zero = 0;
    internal const int Small = 1;
    internal static readonly int[] WarmCharges = [0, 1, 1999, 2000, 4001];
}
